using System.Text.Json;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Separate channels have separate endpoints and can legitimately reuse the same orderCode.
// Each handler verifies the signature using its own recorded payment credentials.
public sealed class PayOsWebhookDispatcher(ICheckoutPaymentService checkout,
    IPlatformFeeStatementService fees)
{
    public async Task DispatchAsync(JsonElement body, CancellationToken ct, bool platformFees = false)
    {
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("orderCode", out var code)
            || code.ValueKind != JsonValueKind.Number || !code.TryGetInt64(out _))
            throw AppException.BadRequest("Invalid payment notification.");
        if (platformFees)
            await fees.ProcessFeeWebhookAsync(body, ct);
        else
            await checkout.WebhookAsync(body, ct);
    }
}
