using System.Security.Cryptography;
using System.Text.Json;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

// A short-lived, unshared link verifies authentication AND the checksum key. No charge is made.
public sealed class PayOsCredentialsValidator(IPayOsApiClient api, IConfiguration configuration, TimeProvider clock)
    : IPayOsCredentialsValidator
{
    public async Task ValidateAsync(PlatformPayOsCredentials credentials, CancellationToken ct = default)
    {
        if (new[] { credentials.ClientId, credentials.ApiKey, credentials.ChecksumKey }.Any(string.IsNullOrWhiteSpace))
            throw AppException.BadRequest("Client ID, API Key and Checksum Key are required.");
        var frontend = configuration["Checkout:FrontendBaseUrl"] ?? "http://localhost:5173";
        if (!Uri.TryCreate(frontend, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw AppException.ServiceUnavailable("The payment return address is not configured.");
        // Random 52-bit order codes avoid collisions with normal payments and concurrent probes.
        var orderCode = (BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8)) & 0x000FFFFFFFFFFFFF) + 1;
        var amount = (long)MoneyLimits.MinAmountVnd;
        var returnUrl = frontend.TrimEnd('/') + "/";
        var cancelUrl = returnUrl;
        const string description = "SBVERIFY";
        var signatureData = FormattableString.Invariant($"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}");
        try
        {
            var data = await api.SendAsync(HttpMethod.Post, "v2/payment-requests", credentials, new
            {
                orderCode, amount, description, cancelUrl, returnUrl,
                expiredAt = clock.GetUtcNow().AddMinutes(5).ToUnixTimeSeconds(),
                signature = PayOsCheckoutGateway.Sign(signatureData, credentials.ChecksumKey)
            }, ct);
            if (!data.TryGetProperty("orderCode", out var code) || !code.TryGetInt64(out var actualCode) || actualCode != orderCode
                || !data.TryGetProperty("amount", out var total) || !total.TryGetInt64(out var actualAmount) || actualAmount != amount)
                throw AppException.ServiceUnavailable("Unable to verify PayOS configuration. Please retry.");
            var cancelled = await api.SendAsync(HttpMethod.Post, $"v2/payment-requests/{orderCode}/cancel", credentials,
                new { cancellationReason = "SaveBite credential verification" }, ct);
            if (!cancelled.TryGetProperty("status", out var status) || status.GetString() != "CANCELLED")
                throw AppException.ServiceUnavailable("Unable to finish PayOS verification. Please retry.");
        }
        catch (AppException ex) when (ex.ErrorCode == ErrorCodes.PaymentCredentialsInvalid)
        {
            var message = ex is PayOsCredentialException { RequestRejected: false }
                ? "The PayOS response signature does not match. Please check that the Checksum Key belongs to the same payment channel as the Client ID and API Key, then verify again."
                : "The PayOS keys are incorrect or do not match. Please check and update the Client ID, API Key and Checksum Key from the same payment channel, then verify again.";
            throw AppException.BadRequest(message, ErrorCodes.PaymentCredentialsInvalid);
        }
        catch (AppException ex) when (ex.ErrorCode.StartsWith("PAYOS_", StringComparison.Ordinal))
        {
            var message = ex.ErrorCode switch
            {
                ErrorCodes.PayOsConnectionFailed => "Unable to connect to PayOS to verify your keys. This does not mean the keys are incorrect. Please try again shortly.",
                ErrorCodes.PayOsTimeout => "PayOS verification timed out. Your keys have not been verified. Please try again.",
                ErrorCodes.PayOsRateLimited => "Too many verification requests. Please wait a moment and verify again.",
                ErrorCodes.PayOsProviderUnavailable => "PayOS is temporarily unavailable. Your keys have not been verified. Please try again later.",
                ErrorCodes.PayOsRequestRejected => "PayOS could not complete the verification request. This does not confirm that the keys are incorrect. Please try again or contact support.",
                _ => "PayOS returned an incomplete or invalid verification response. Your keys have not been verified. Please try again."
            };
            throw new AppException(message, ex.ErrorCode, ex.StatusCode);
        }
    }
}
