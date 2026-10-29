using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Repositories.Interfaces;

public interface IRefundRepository
{
    Task<bool> HasCustomerRequestAsync(Guid orderId, CancellationToken ct);
    Task<int> ShopPendingCountAsync(Guid shopId, CancellationToken ct);
    Task<int> PendingDetailsCountAsync(Guid userId, CancellationToken ct);
    Task<Refund?> GetAsync(Guid id, bool tracked, CancellationToken ct);
    Task<List<Refund>> ForOrderAsync(Guid orderId, CancellationToken ct);
    Task<Order?> OrderAsync(Guid id, CancellationToken ct);
    Task<(List<Refund> Items, int Total)> PageAsync(Guid userId, Guid? shopId, RefundStatus? status, RefundType? type, int page, int size, CancellationToken ct);
    Task<Guid?> SystemRefundIdAsync(Guid orderId, CancellationToken ct);
    Task<User> UserAsync(Guid id, CancellationToken ct);
    Task<TrustOrderProcessing?> ProcessingAsync(Guid orderId, CancellationToken ct);
    Task<bool> LegacyCreditAsync(Guid orderId, CancellationToken ct);
    Task<OrderPlatformFee?> FeeAsync(Guid orderId, CancellationToken ct);
    void Add<T>(T entity) where T : class;
}
