using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

// Expose customer-owned order history, details and cancellation.
public interface IOrderService
{
    Task<OrderHistoryPageResponse> PageAsync(Guid userId, string? status, string? cursor, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<CheckoutOrderResponse>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<CheckoutOrderResponse> GetAsync(Guid userId, Guid orderId, CancellationToken ct = default);
    Task<CheckoutOrderResponse> CancelAsync(Guid userId, Guid orderId, CancellationToken ct = default);
}
