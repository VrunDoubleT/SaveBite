using System.Text.Json;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Services.Interfaces;

// Normalize provider status, amounts and link data for payment reconciliation.
public sealed record GatewayPayment(string Status, long OrderCode, decimal Amount, decimal AmountPaid, string? LinkId, string? CheckoutUrl, string? QrCode = null);
// Abstract real and simulated payment creation, verification and closure.
public interface ICheckoutPaymentGateway
{
    bool IsConfigured(ShopPaymentConfig? config);
    Task<GatewayPayment> CreateAsync(Order order, Payment payment, ShopPaymentConfig config, CancellationToken ct);
    Task<GatewayPayment> GetAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct);
    Task<GatewayPayment> CancelAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct);
    bool VerifyWebhook(JsonElement body, ShopPaymentConfig config);
}
