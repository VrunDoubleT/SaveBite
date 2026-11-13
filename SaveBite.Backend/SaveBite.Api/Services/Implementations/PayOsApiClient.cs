using System.Net.Http.Json;
using System.Text.Json;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Services.Implementations;
public sealed class PayOsApiClient(IHttpClientFactory clients, ILogger<PayOsApiClient>? logger = null) : IPayOsApiClient
{
    private readonly IHttpClientFactory _clients = clients;
    public async Task<JsonElement> SendAsync(HttpMethod method, string path, PlatformPayOsCredentials config, object? body, CancellationToken ct)
    {
        if (new[] { config.ClientId, config.ApiKey, config.ChecksumKey }.Any(string.IsNullOrWhiteSpace)) throw InvalidCredentials();
        try
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("x-client-id", config.ClientId);
            request.Headers.Add("x-api-key", config.ApiKey);
            if (body != null) request.Content = JsonContent.Create(body);
            using var response = await _clients.CreateClient("PayOsCheckout").SendAsync(request, ct);
            // Authentication can fail with an empty/non-JSON body; classify before parsing.
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw InvalidCredentials();
            logger?.LogInformation("PayOS request {Method} {Path} completed with HTTP {StatusCode}.", method.Method, path, (int)response.StatusCode);
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                throw ProviderError("Too many PayOS requests. Please wait a moment before trying again.", ErrorCodes.PayOsRateLimited);
            if ((int)response.StatusCode >= 500)
                throw ProviderError("PayOS is temporarily unavailable. Please try again shortly.", ErrorCodes.PayOsProviderUnavailable);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound && method == HttpMethod.Get)
                return JsonSerializer.SerializeToElement(new { status = "NOT_FOUND" });
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = json.RootElement;
            if (!response.IsSuccessStatusCode || !root.TryGetProperty("code", out var code) || code.GetString() != "00")
            {
                if ((int)response.StatusCode < 500 && response.StatusCode != System.Net.HttpStatusCode.TooManyRequests
                    && root.TryGetProperty("desc", out var description) && description.ValueKind == JsonValueKind.String
                    && IsCredentialRejection(description.GetString()!))
                    throw InvalidCredentials();
                if (path == "confirm-webhook")
                {
                    var reason = root.TryGetProperty("desc", out var desc) && desc.ValueKind == JsonValueKind.String
                        ? desc.GetString() ?? "No reason supplied" : "No reason supplied";
                    foreach (var secret in new[] { config.ClientId, config.ApiKey, config.ChecksumKey })
                        reason = reason.Replace(secret, "[redacted]", StringComparison.Ordinal);
                    reason = reason.Replace('\r', ' ').Replace('\n', ' ');
                    if (reason.Length > 500) reason = reason[..500];
                    throw ProviderError($"PayOS rejected webhook registration (HTTP {(int)response.StatusCode}): {reason}", ErrorCodes.PayOsRequestRejected);
                }
                throw ProviderError("PayOS rejected this request. Please try again or contact support if the issue continues.", ErrorCodes.PayOsRequestRejected);
            }
            var data = root.GetProperty("data");
            // confirm-webhook has no response signature in the PayOS API contract.
            // Authentication uses the channel keys; the caller validates the confirmed URL.
            if (path == "confirm-webhook") return data.Clone();
            if (!root.TryGetProperty("signature", out var signature) || signature.ValueKind != JsonValueKind.String
                || string.IsNullOrEmpty(signature.GetString()))
                throw ProviderError("The PayOS response could not be verified. Please retry.", ErrorCodes.PayOsInvalidResponse);
            if (!PayOsCheckoutGateway.Verify(data, signature.GetString(), config.ChecksumKey))
                throw InvalidCredentials(requestRejected: false);
            return data.Clone();
        }
        catch (HttpRequestException ex)
        {
            // Do not log headers, credentials, request bodies or provider response bodies.
            logger?.LogWarning("PayOS connection failed. NetworkError: {NetworkError}, HTTP status: {StatusCode}.",
                ex.HttpRequestError, ex.StatusCode);
            throw ProviderError("Unable to connect to PayOS. Please try again shortly.", ErrorCodes.PayOsConnectionFailed);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger?.LogWarning("PayOS request timed out.");
            throw ProviderError("PayOS did not respond in time. Please try again.", ErrorCodes.PayOsTimeout);
        }
        catch (KeyNotFoundException) { throw InvalidResponse(); }
        catch (InvalidOperationException) { throw InvalidResponse(); }
        catch (JsonException) { throw InvalidResponse(); }
    }

    private static AppException ProviderError(string message, string code)
        => new(message, code, System.Net.HttpStatusCode.ServiceUnavailable);
    private static AppException InvalidResponse()
        => ProviderError("PayOS returned an incomplete or invalid response. Please try again.", ErrorCodes.PayOsInvalidResponse);

    private static AppException InvalidCredentials(bool requestRejected = true) => new PayOsCredentialException(requestRejected);

    // Inspect only explicit authentication/signature errors, never generic provider failures.
    private static bool IsCredentialRejection(string description)
    {
        var text = description.ToLowerInvariant();
        var credential = text.Contains("signature") || text.Contains("api key") || text.Contains("api-key")
            || text.Contains("client id") || text.Contains("client-id") || text.Contains("checksum")
            || text.Contains("chữ ký");
        var invalid = text.Contains("invalid") || text.Contains("incorrect") || text.Contains("not match") || text.Contains("mismatch")
            || text.Contains("không hợp lệ") || text.Contains("không đúng");
        return text.TrimEnd('.') == "unauthorized" || credential && invalid;
    }

}
