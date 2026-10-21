using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

// Carry the authoritative customer, cart, trust policy and payment configuration for checkout.
public sealed record CheckoutState(User User, IReadOnlyList<CartItem> Items, TrustLevel? Level,
    bool IsDefaultLevel, bool PermanentlyLocked, bool Recovering, ShopPaymentConfig? PaymentConfig);
// Coordinate authoritative checkout state, idempotency, stock and transactional order changes.
public interface ICheckoutRepository
{
    Task<CheckoutState> GetStateAsync(Guid userId, Guid shopId, CancellationToken ct);
    Task<Order?> FindRequestAsync(Guid userId, Guid requestId, Guid quoteId, CancellationToken ct);
    Task<int> OrderCodeMaximumAsync(Guid shopId, DateOnly date, CancellationToken ct);
    Task<Order?> GetOrderAsync(Guid orderId, Guid? userId, CancellationToken ct);
    Task<(IReadOnlyList<Order> Orders, bool HasMore, IReadOnlyDictionary<string, int> Counts)> GetOrderPageAsync(Guid userId, SaveBite.Backend.Models.Enums.OrderStatus? status, DateTime? beforeCreatedAt, Guid? beforeId, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetOrdersAsync(Guid userId, CancellationToken ct);
    Task<Guid?> FindGatewayOrderAsync(long gatewayCode, CancellationToken ct);
    void AddOrder(Order order);
    void AddHistory(OrderStatusHistory history);
    Task ReserveAsync(IReadOnlyList<CartItem> items, CancellationToken ct);
    Task ReleaseCancelledInventoryAsync(Order order, CancellationToken ct);
    Task CompleteInventoryAsync(Order order, bool sold, CancellationToken ct);
    Task RemoveShopCartAsync(Guid userId, Guid shopId, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task<T> WithUserLockAsync<T>(Guid userId, Guid shopId, Func<Task<T>> action, CancellationToken ct);
    Task<T> WithOrderLockAsync<T>(Guid orderId, Func<Task<T>> action, CancellationToken ct);
}
