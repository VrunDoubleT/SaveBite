using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

// Read grouped cart data and persist mutations under a customer row lock.
public interface ICartRepository
{
    Task<bool> IsShopOperatorAsync(Guid userId, Guid shopId, CancellationToken ct);
    Task<(IReadOnlyList<CartItem> Items, bool HasMore, Guid? LastShopId, DateTime? LastAddedAt)> GetPageAsync(Guid userId, Guid? afterShopId, DateTime? afterAddedAt, int pageSize, CancellationToken ct);
    Task<SaveBite.Backend.Models.Responses.CartSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<CartItem>> GetItemsAsync(Guid userId, Guid? shopId = null, CancellationToken cancellationToken = default);
    Task<CartItem?> FindItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task<CartItem?> FindVariantItemAsync(Guid userId, Guid variantId, CancellationToken cancellationToken = default);
    Task<FlashDealVariant?> GetVariantAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> RemoveFlashDealItemsAsync(Guid flashDealId, CancellationToken ct);
    void Add(CartItem item);
    void Remove(CartItem item);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> WithUserLockAsync<T>(Guid userId, Func<Task<T>> action, CancellationToken cancellationToken = default);
}
