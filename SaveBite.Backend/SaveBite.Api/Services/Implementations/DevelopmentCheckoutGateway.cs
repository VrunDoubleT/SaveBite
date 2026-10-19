using System.Text.Json;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// State is stored in the existing payment row so retries and server restarts are durable.
public sealed class DevelopmentCheckoutGateway : ICheckoutPaymentGateway
{
    public const string Prefix = "simulation:";
    // Allow simulated payments without requiring real shop payment credentials.
    public bool IsConfigured(ShopPaymentConfig? config) => true;
    // Create a local test-payment link only for a payment marked as simulated.
    public Task<GatewayPayment> CreateAsync(Order order, Payment payment, ShopPaymentConfig config, CancellationToken ct)
    {
        if (payment.GatewayTransactionId?.StartsWith(Prefix) != true)
            throw AppException.Conflict("A real payment cannot be simulated.");
        payment.GatewayTransactionId ??= Prefix + "PENDING";
        payment.GatewayCheckoutUrl = $"/checkout/simulation?orderId={order.Id}"
            + (payment.PaymentType == SaveBite.Backend.Models.Enums.PaymentType.Remaining ? $"&paymentId={payment.Id}" : "");
        return GetAsync(payment, config, ct);
    }

    // Read the durable simulated provider outcome from the existing payment row.
    public Task<GatewayPayment> GetAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct)
    {
        if (payment.GatewayTransactionId?.StartsWith(Prefix) != true)
            throw AppException.Conflict("This payment is not a simulated payment. Switch back to PayOS to reconcile real payments.");
        var status = payment.GatewayTransactionId[Prefix.Length..];
        return Task.FromResult(new GatewayPayment(status, payment.GatewayOrderCode ?? 0, payment.Amount,
            status == "PAID" ? payment.Amount : 0, payment.GatewayTransactionId, payment.GatewayCheckoutUrl));
    }
    
    // Close a pending simulated payment without overwriting an already selected outcome.
    public Task<GatewayPayment> CancelAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct)
    {
        if (payment.GatewayTransactionId == Prefix + "PENDING") payment.GatewayTransactionId = Prefix + "CANCELLED";
        return GetAsync(payment, config, ct);
    }
    // Reject external webhooks because simulation outcomes are selected through authenticated dev APIs.
    public bool VerifyWebhook(JsonElement body, ShopPaymentConfig config) => false;
}
