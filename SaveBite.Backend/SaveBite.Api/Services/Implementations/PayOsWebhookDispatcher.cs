using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Route by the recorded payment, even when a shared PayOS channel was registered via another flow.
// Each handler still verifies the signature using that payment's own credentials.
public sealed class PayOsWebhookDispatcher(AppDbContext db, ICheckoutPaymentService checkout,
    IPlatformFeeStatementService fees)
{
    public async Task DispatchAsync(JsonElement body, CancellationToken ct)
    {
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("orderCode", out var code)
            || code.ValueKind != JsonValueKind.Number || !code.TryGetInt64(out var orderCode))
            throw AppException.BadRequest("Invalid payment notification.");
        var gatewayCode = orderCode.ToString();
        if (await db.PlatformFeePayments.AsNoTracking().AnyAsync(p => p.GatewayTransactionId == gatewayCode, ct))
            await fees.ProcessFeeWebhookAsync(body, ct);
        else
            await checkout.WebhookAsync(body, ct);
    }
}
