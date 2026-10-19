using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
namespace SaveBite.Backend.Services.Interfaces;

// Define authorized shop fulfillment, remaining payment and refund operations.
public interface IShopOrderService
{
    Task<int> PendingCountAsync(Guid userId, Guid shopId, CancellationToken ct = default);
    Task<Guid> OwnerShopAsync(Guid userId, CancellationToken ct = default);
    Task<ShopOrderPageResponse> PageAsync(Guid userId, Guid shopId, string? status, string? search, int page, int size, CancellationToken ct = default, DateOnly? orderDate = null);
    Task<ShopOrderResponse> GetAsync(Guid userId, Guid shopId, Guid orderId, CancellationToken ct = default);
    Task<ShopOrderResponse> ActionAsync(Guid userId, Guid shopId, Guid orderId, string action, string? reason = null, CancellationToken ct = default, bool cashReceived = false);
    Task<ShopOrderResponse> RemainingPaymentAsync(Guid userId, Guid shopId, Guid orderId, CancellationToken ct = default);
    Task ExpireConfirmationAsync(Guid orderId, CancellationToken ct = default);
    Task MarkNoShowAsync(Guid orderId, CancellationToken ct = default);
    Task<OrderRefundResponse?> CustomerRefundAsync(Guid userId, Guid orderId, CancellationToken ct = default);
    Task<OrderRefundResponse> DestinationAsync(Guid userId, Guid orderId, RefundDestinationRequest request, CancellationToken ct = default);
    Task<OrderRefundResponse> VerifyDestinationAsync(Guid userId, Guid orderId, string code, CancellationToken ct = default);
    Task<OrderRefundResponse> TransferAsync(Guid userId, Guid shopId, Guid orderId, string reference, CancellationToken ct = default);
}
