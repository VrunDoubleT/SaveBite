using System.Net.Http.Json;
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
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;

    public PayOsCheckoutGateway(
        IHttpClientFactory clients,
        IConfiguration configuration)
    {
        _clients = clients;
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
        var frontend = _configuration["Checkout:FrontendBaseUrl"] ?? "http://localhost:5173";
        if (!Uri.TryCreate(frontend, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("https" or "http"))
            throw AppException.ServiceUnavailable("The payment return address is not configured.");
        var returnUrl = $"{frontend.TrimEnd('/')}/checkout?orderId={order.Id}";
        var cancelUrl = returnUrl + "&cancelled=1";
        var code = payment.GatewayOrderCode ?? throw AppException.InternalError();
        if (payment.Amount <= 0 || decimal.Truncate(payment.Amount) != payment.Amount || payment.Amount > long.MaxValue)
            throw AppException.BadRequest("Online payment amounts must be positive whole VND values.");
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
        return Parse(await SendAsync(HttpMethod.Get, $"v2/payment-requests/{payment.GatewayOrderCode}", config, null, ct), payment, false);
    }
    // Request PayOS link closure and return its verified payment outcome.
    public async Task<GatewayPayment> CancelAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct)
    {
        EnsureReal(payment);
        return Parse(await SendAsync(HttpMethod.Post, $"v2/payment-requests/{payment.GatewayOrderCode}/cancel", config,
            new { cancellationReason = "Order payment window closed or order cancelled" }, ct), payment, false);
    }
    // Reject simulated payment rows before contacting the real payment provider.
    private static void EnsureReal(Payment payment)
    {
        if (payment.GatewayTransactionId?.StartsWith(DevelopmentCheckoutGateway.Prefix) == true)
            throw AppException.Conflict("Simulated payments cannot be sent to PayOS. Use Development simulation mode to resolve this order.");
    }

    // Send an authenticated PayOS request and validate the provider's response and signature.
    private async Task<JsonElement> SendAsync(HttpMethod method, string path, ShopPaymentConfig config, object? body, CancellationToken ct)
    {
        if (!IsConfigured(config)) throw AppException.ServiceUnavailable("This shop has not configured online payments yet.");
        try
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("x-client-id", config.PayosClientId);
            request.Headers.Add("x-api-key", config.PayosApiKey);
            if (body != null) request.Content = JsonContent.Create(body);
            using var response = await _clients.CreateClient("PayOsCheckout").SendAsync(request, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound && method == HttpMethod.Get)
                return JsonSerializer.SerializeToElement(new { status = "NOT_FOUND" });
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = json.RootElement;
            if (!response.IsSuccessStatusCode || !root.TryGetProperty("code", out var code) || code.GetString() != "00")
                throw AppException.ServiceUnavailable("The payment provider could not process this request. Your payment has not been confirmed; please retry.");
            var data = root.GetProperty("data");
            if (!root.TryGetProperty("signature", out var signature) || !Verify(data, signature.GetString(), config.PayosChecksumKey))
                throw AppException.ServiceUnavailable("The payment provider response could not be verified. Please retry.");
            return data.Clone();
        }
        catch (HttpRequestException) { throw AppException.ServiceUnavailable("The payment provider is temporarily unavailable. You can retry payment from your order."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw AppException.ServiceUnavailable("Payment provider timeout. Check your order before retrying."); }
        catch (KeyNotFoundException) { throw AppException.ServiceUnavailable("The payment provider returned an incomplete response."); }
        catch (InvalidOperationException) { throw AppException.ServiceUnavailable("The payment provider returned an invalid response."); }
        catch (JsonException) { throw AppException.ServiceUnavailable("The payment provider returned an invalid response."); }
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
        => body.ValueKind == JsonValueKind.Object && body.TryGetProperty("data", out var data)
            && body.TryGetProperty("signature", out var signature) && signature.ValueKind == JsonValueKind.String
            && Verify(data, signature.GetString(), config.PayosChecksumKey);

    // Calculate the HMAC SHA256 signature for canonical payment data.
    public static string Sign(string value, string key)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    // Compare the expected and received signatures without leaking comparison timing.
    private static bool Verify(JsonElement data, string? signature, string key)
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
