using SaveBite.Backend.Constants;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Create and reconcile real PayOS links with signed request and response validation.
public sealed class PayOsCheckoutGateway : ICheckoutPaymentGateway
{
    private readonly IPayOsApiClient _api;
    private readonly IPayOsCredentialProtector _protector;
    private readonly IConfiguration _configuration;

    public PayOsCheckoutGateway(
        IPayOsApiClient api,
        IPayOsCredentialProtector protector,
        IConfiguration configuration)
    {
        _api = api;
        _protector = protector;
        _configuration = configuration;
    }

    // Require non-placeholder PayOS credentials before accepting real online payments.
    public bool IsConfigured(ShopPaymentConfig? config)
        => config != null && new[] { config.PayosClientId, config.PayosApiKey, config.PayosChecksumKey }
            .All(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("seed-", StringComparison.OrdinalIgnoreCase));

    // Create a signed PayOS payment link using the recorded amount and payment deadline.
    public async Task<GatewayPayment> CreateAsync(Order order, Payment payment, ShopPaymentConfig config, CancellationToken ct)
    {
        EnsureReal(payment);
        config = PaymentConfig(payment, config);
        var frontend = _configuration["Checkout:FrontendBaseUrl"] ?? "http://localhost:5173";
        if (!Uri.TryCreate(frontend, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("https" or "http"))
            throw AppException.ServiceUnavailable("The payment return address is not configured.");
        var returnUrl = $"{frontend.TrimEnd('/')}/checkout?orderId={order.Id}";
        var cancelUrl = returnUrl + "&cancelled=1";
        var code = payment.GatewayOrderCode ?? throw AppException.InternalError();
        if (payment.Amount < MoneyLimits.MinAmountVnd || decimal.Truncate(payment.Amount) != payment.Amount || payment.Amount > long.MaxValue)
            throw AppException.BadRequest($"Online payment amounts must be whole VND values of at least {MoneyLimits.MinAmountVnd:N0}.");
        var amount = (long)payment.Amount;
        var description = $"SB{code % 10000000:0000000}";
        var signatureData = FormattableString.Invariant($"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={code}&returnUrl={returnUrl}");
        var request = new
        {
            orderCode = code, amount, description, cancelUrl, returnUrl,
            expiredAt = new DateTimeOffset((payment.PaymentType == SaveBite.Backend.Models.Enums.PaymentType.Remaining ? order.HardDeadlineAt : order.PaymentExpiresAt)!.Value).ToUnixTimeSeconds(),
            signature = Sign(signatureData, config.PayosChecksumKey)
        };
        var data = await SendAsync(HttpMethod.Post, "v2/payment-requests", config, request, ct);
        return Parse(data, payment, true);
    }

    // Query PayOS for the authoritative status and collected amount of this payment.
    public async Task<GatewayPayment> GetAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct)
    {
        EnsureReal(payment);
        return Parse(await SendForPaymentAsync(HttpMethod.Get, $"v2/payment-requests/{payment.GatewayOrderCode}", payment, config, null, ct), payment, false);
    }
    // Request PayOS link closure and return its verified payment outcome.
    public async Task<GatewayPayment> CancelAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct)
    {
        EnsureReal(payment);
        return Parse(await SendForPaymentAsync(HttpMethod.Post, $"v2/payment-requests/{payment.GatewayOrderCode}/cancel", payment, config,
            new { cancellationReason = "Order payment window closed or order cancelled" }, ct), payment, false);
    }
    // Reject simulated payment rows before contacting the real payment provider.
    private static void EnsureReal(Payment payment)
    {
        if (payment.GatewayTransactionId?.StartsWith(DevelopmentCheckoutGateway.Prefix) == true)
            throw AppException.Conflict("Simulated payments cannot be sent to PayOS. Use Development simulation mode to resolve this order.");
    }

    private Task<JsonElement> SendAsync(HttpMethod method, string path, ShopPaymentConfig config, object? body, CancellationToken ct)
        => _api.SendAsync(method, path, new(config.PayosClientId, config.PayosApiKey, config.PayosChecksumKey), body, ct);

    private async Task<JsonElement> SendForPaymentAsync(HttpMethod method, string path, Payment payment,
        ShopPaymentConfig current, object? body, CancellationToken ct)
    {
        var original = PaymentConfig(payment, current);
        try { return await SendAsync(method, path, original, body, ct); }
        catch (AppException ex) when (ex.ErrorCode == ErrorCodes.PaymentCredentialsInvalid
            && original.PayosClientId == current.PayosClientId
            && (original.PayosApiKey != current.PayosApiKey || original.PayosChecksumKey != current.PayosChecksumKey))
        {
            return await SendAsync(method, path, current, body, ct);
        }
    }

    private ShopPaymentConfig PaymentConfig(Payment payment, ShopPaymentConfig current)
    {
        if (payment.ProtectedPayOsCredentials is null) return current;
        var credentials = _protector.Unprotect(payment.ProtectedPayOsCredentials);
        return new ShopPaymentConfig { ShopId = current.ShopId, PayosClientId = credentials.ClientId,
            PayosApiKey = credentials.ApiKey, PayosChecksumKey = credentials.ChecksumKey };
    }

    // Convert provider payment data into the shared gateway result used by reconciliation.
    private static GatewayPayment Parse(JsonElement data, Payment payment, bool created)
    {
        if (data.TryGetProperty("status", out var missingStatus) && missingStatus.GetString() == "NOT_FOUND")
            return new("NOT_FOUND", payment.GatewayOrderCode ?? 0, payment.Amount, 0, null, null);
        if (!data.TryGetProperty("orderCode", out var codeValue) || codeValue.ValueKind != JsonValueKind.Number || !codeValue.TryGetInt64(out var code)
            || !data.TryGetProperty("amount", out var amountValue) || amountValue.ValueKind != JsonValueKind.Number || !amountValue.TryGetDecimal(out var amount)
            || !data.TryGetProperty("status", out var stateValue) || stateValue.ValueKind != JsonValueKind.String)
            throw AppException.ServiceUnavailable("The payment provider returned incomplete payment data.");
        if (code != payment.GatewayOrderCode || amount != payment.Amount)
            throw AppException.Conflict("Payment details do not match this order. Please contact support.");
        var status = data.GetProperty("status").GetString() ?? "UNKNOWN";
        var idName = created ? "paymentLinkId" : "id";
        var id = data.TryGetProperty(idName, out var linkId) ? linkId.GetString() : null;
        var url = data.TryGetProperty("checkoutUrl", out var checkout) ? checkout.GetString() : id != null ? $"https://pay.payos.vn/web/{Uri.EscapeDataString(id)}" : null;
        if (url != null && (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || !string.IsNullOrEmpty(uri.UserInfo) || !(uri.Host == "payos.vn" || uri.Host.EndsWith(".payos.vn", StringComparison.OrdinalIgnoreCase))))
            throw AppException.ServiceUnavailable("The payment provider returned an invalid checkout URL.");
        var paid = data.TryGetProperty("amountPaid", out var paidValue) ? paidValue.GetDecimal() : 0;
        return new(status, code, amount, paid, id, url, data.TryGetProperty("qrCode", out var qr) && qr.ValueKind == JsonValueKind.String ? qr.GetString() : null);
    }

    // Verify a PayOS webhook signature with the shop's checksum key before processing it.
    public bool VerifyWebhook(JsonElement body, ShopPaymentConfig config)
    //more
        => VerifyWebhookData(body, config.PayosChecksumKey);
    // Verify a PayOS webhook payload with a checksum key.
    public static bool VerifyWebhookData(JsonElement body, string checksumKey)
    // end
        => body.ValueKind == JsonValueKind.Object && body.TryGetProperty("data", out var data)
            && body.TryGetProperty("signature", out var signature) && signature.ValueKind == JsonValueKind.String
          //  && Verify(data, signature.GetString(), config.PayosChecksumKey);
          && Verify(data, signature.GetString(), checksumKey);
    // Calculate the HMAC SHA256 signature for canonical payment data.
    public static string Sign(string value, string key)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    // Compare the expected and received signatures without leaking comparison timing.
    public static bool Verify(JsonElement data, string? signature, string key)//more
   // private static bool Verify(JsonElement data, string? signature, string key)
    {
        if (data.ValueKind != JsonValueKind.Object || string.IsNullOrEmpty(signature)) return false;
        var canonical = string.Join("&", data.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => $"{x.Name}={CanonicalValue(x.Value)}"));
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Sign(canonical, key)), Convert.FromHexString(signature)); }
        catch (FormatException) { return false; }
    }
    // Convert a JSON value into the canonical representation used for PayOS signatures.
    private static string CanonicalValue(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return "";
        if (value.ValueKind == JsonValueKind.String) return value.GetString() ?? "";
        if (value.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array)) return value.GetRawText();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            WriteSorted(writer, value);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    // Write JSON properties in a stable order so signature calculation is deterministic.
    private static void WriteSorted(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var p in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
            { writer.WritePropertyName(p.Name); WriteSorted(writer, p.Value); }
            writer.WriteEndObject();
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) WriteSorted(writer, item); writer.WriteEndArray();
        }
        else value.WriteTo(writer);
    }
}
