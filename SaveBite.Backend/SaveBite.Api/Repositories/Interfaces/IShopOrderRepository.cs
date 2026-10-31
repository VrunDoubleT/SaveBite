using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

// Provide shop access checks, order queries and fulfillment audit persistence.
public interface IShopOrderRepository
{
    Task<bool> RequireAccessAsync(Guid userId, Guid shopId, CancellationToken ct);
    Task<SaveBite.Backend.Models.DTOs.ShopOrderActor> ActorAsync(Guid userId, Guid shopId, CancellationToken ct);
    Task<int> PendingCountAsync(Guid shopId, CancellationToken ct);
    Task<Guid> OwnerShopAsync(Guid userId, CancellationToken ct);
    Task<Order?> GetAsync(Guid shopId, Guid orderId, CancellationToken ct);
    Task<(IReadOnlyList<Order> Items, int TotalItems, IReadOnlyDictionary<string, int> Counts)> PageAsync(Guid shopId,
        string? status, string? search, int page, int size, CancellationToken ct, DateOnly? orderDate = null);
    Task<Guid?> CustomerRecipientAsync(Guid shopId, Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<Guid>> RealtimeRecipientsAsync(Guid shopId, CancellationToken ct);
    void AddPayment(Payment payment);
    void AddRefund(Refund refund);
    void AddActivity(StaffActivityLog activity);
    Task<OrderPlatformFee?> GetOrderPlatformFeeAsync(Guid orderId, CancellationToken ct);
    void AddFeeAdjustment(PlatformFeeAdjustment adjustment);
    void AddStatementHistory(PlatformFeeStatementHistory history);

}
