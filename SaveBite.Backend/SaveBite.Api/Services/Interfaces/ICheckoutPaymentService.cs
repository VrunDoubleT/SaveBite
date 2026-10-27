using System.Text.Json;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

// Reconcile initial and remaining payments with order and inventory state.
public interface ICheckoutPaymentService
{
    Task<CheckoutOrderResponse> ReconcileAsync(Guid userId, Guid orderId, CancellationToken ct = default);
    Task<CheckoutOrderResponse> StartAsync(Guid userId, Guid orderId, CancellationToken ct = default);
    Task<CheckoutOrderResponse> CancelAsync(Guid userId, Guid orderId, CancellationToken ct = default);
    Task WebhookAsync(JsonElement body, CancellationToken ct = default);
    Task<CheckoutOrderResponse> ReconcileOrderAsync(Guid orderId, bool createRemainingLink = false, CancellationToken ct = default);
    Task ExpireAsync(Guid orderId, CancellationToken ct = default);
}
