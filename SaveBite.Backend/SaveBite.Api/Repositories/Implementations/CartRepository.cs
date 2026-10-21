using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

// Read cart data and persist customer cart changes through Entity Framework Core.
public sealed class CartRepository : ICartRepository
{
    private readonly AppDbContext _dbContext;

    public CartRepository(AppDbContext db)
    {
        _dbContext = db;
    }

    // Check the actual shop relationship rather than the account's primary role.
    public async Task<bool> IsShopOperatorAsync(Guid userId, Guid shopId, CancellationToken ct)
        => await _dbContext.Shops.AsNoTracking().AnyAsync(x => x.Id == shopId && x.OwnerUserId == userId, ct)
            || await _dbContext.ShopStaffMembers.AsNoTracking().AnyAsync(x => x.ShopId == shopId && x.UserId == userId
                && x.Status == SaveBite.Backend.Models.Enums.ShopStaffStatus.Active, ct);

    // Load this customer's cart, optionally limited to one shop, with newest additions first.
    public async Task<IReadOnlyList<CartItem>> GetItemsAsync(Guid userId, Guid? shopId = null, CancellationToken cancellationToken = default)
    {
        // Load shop, product images, and variant attributes without tracking read-only entities.
        return await _dbContext.CartItems.AsNoTracking()
            .Where(x => x.UserId == userId && (!shopId.HasValue || x.FlashDealVariant.FlashDeal.ShopId == shopId))
            .Include(x => x.FlashDealVariant).ThenInclude(x => x.FlashDeal).ThenInclude(x => x.Shop)
            .Include(x => x.FlashDealVariant).ThenInclude(x => x.FlashDeal).ThenInclude(x => x.Product).ThenInclude(x => x.Images)
            .Include(x => x.FlashDealVariant).ThenInclude(x => x.Variant).ThenInclude(x => x.VariantValues)
                .ThenInclude(x => x.AttributeValue).ThenInclude(x => x.Attribute)
            .AsSplitQuery().OrderByDescending(x => x.AddedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }

    // Page by complete shops using each shop's latest addition timestamp and its ID as the cursor.
    public async Task<(IReadOnlyList<CartItem> Items, bool HasMore, Guid? LastShopId, DateTime? LastAddedAt)> GetPageAsync(Guid userId, Guid? afterShopId, DateTime? afterAddedAt, int pageSize, CancellationToken ct)
    {
        // Group cart items by shop and find the most recent addition in each group.
        var shops = _dbContext.CartItems.Where(x => x.UserId == userId)
            .GroupBy(x => x.FlashDealVariant.FlashDeal.ShopId)
            .Select(group => new { ShopId = group.Key, AddedAt = group.Max(x => x.AddedAt) });
        // Use the timestamp and shop ID together so equal timestamps do not skip shops.
        if (afterShopId.HasValue && afterAddedAt.HasValue)
            shops = shops.Where(x => x.AddedAt < afterAddedAt.Value
                || (x.AddedAt == afterAddedAt.Value && x.ShopId.CompareTo(afterShopId.Value) > 0));
        // Fetch one extra shop to detect another page without loading every shop.
        var page = await shops.OrderByDescending(x => x.AddedAt).ThenBy(x => x.ShopId).Take(pageSize + 1).ToListAsync(ct);
        var hasMore = page.Count > pageSize;
        page = page.Take(pageSize).ToList();
        var ids = page.Select(x => x.ShopId).ToList();
        // Load every item for the selected shops so pagination never splits a checkout group.
        var items = await _dbContext.CartItems.AsNoTracking().Where(x => x.UserId == userId && ids.Contains(x.FlashDealVariant.FlashDeal.ShopId))
            .Include(x => x.FlashDealVariant).ThenInclude(x => x.FlashDeal).ThenInclude(x => x.Shop)
            .Include(x => x.FlashDealVariant).ThenInclude(x => x.FlashDeal).ThenInclude(x => x.Product).ThenInclude(x => x.Images)
            .Include(x => x.FlashDealVariant).ThenInclude(x => x.Variant).ThenInclude(x => x.VariantValues)
                .ThenInclude(x => x.AttributeValue).ThenInclude(x => x.Attribute)
            .AsSplitQuery().OrderBy(x => x.FlashDealVariant.FlashDeal.ShopId).ThenByDescending(x => x.AddedAt).ThenBy(x => x.Id).ToListAsync(ct);
        // Return the last selected shop's sort values so the next page does not depend on that row still existing.
        return (items, hasMore, page.Count > 0 ? page[^1].ShopId : null, page.Count > 0 ? page[^1].AddedAt : null);
    }
    
    // Count quantities and distinct Flash Deals across the whole cart, independent of pagination.
    public async Task<SaveBite.Backend.Models.Responses.CartSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken ct)
    {
        var items = _dbContext.CartItems.AsNoTracking().Where(x => x.UserId == userId);
        return new SaveBite.Backend.Models.Responses.CartSummaryResponse
        {
            TotalQuantity = await items.SumAsync(x => x.Quantity, ct),
            TotalFlashDeals = await items.Select(x => x.FlashDealVariant.FlashDealId).Distinct().CountAsync(ct)
        };
    }

    // Find a tracked cart item belonging to this customer for an update or removal.
    public Task<CartItem?> FindItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
        => _dbContext.CartItems.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == itemId, cancellationToken);

    // Find this customer's existing variant entry so another addition can increase its quantity.
    public Task<CartItem?> FindVariantItemAsync(Guid userId, Guid variantId, CancellationToken cancellationToken = default)
        => _dbContext.CartItems.SingleOrDefaultAsync(x => x.UserId == userId && x.FlashDealVariantId == variantId, cancellationToken);

    // Load the deal, shop, product, and variant needed by the service to validate availability.
    public Task<FlashDealVariant?> GetVariantAsync(Guid variantId, CancellationToken cancellationToken = default)
        => _dbContext.FlashDealVariants.AsNoTracking()
            .Include(x => x.FlashDeal).ThenInclude(x => x.Shop)
            .Include(x => x.FlashDeal).ThenInclude(x => x.Product)
            .Include(x => x.Variant)
            .SingleOrDefaultAsync(x => x.Id == variantId, cancellationToken);

    // Track a new cart item; SaveChangesAsync writes it to the database.
    public void Add(CartItem item) => _dbContext.CartItems.Add(item);
    
    // Mark an existing cart item for deletion on the next save.
    public void Remove(CartItem item) => _dbContext.CartItems.Remove(item);
    
    // Persist pending cart inserts, quantity changes, timestamps, and deletions.
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);

    // Execute a cart mutation in a transaction protected by a PostgreSQL lock on this customer's user row.
    public async Task<T> WithUserLockAsync<T>(Guid userId, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        // Serialize this user's mutations, including concurrent first-time adds.
        // Stock is checked, not reserved: adding to a cart does not hold inventory.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM users WHERE id = {userId} FOR UPDATE", cancellationToken);
        // Run the mutation while other operations using the same user-row lock wait for this transaction.
        var result = await action();
        // Commit saved changes and release the lock; disposing an uncommitted transaction rolls it back.
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    // Delete only this deal's cart rows and return owners from rows actually deleted for idempotent delivery.
    public async Task<IReadOnlyList<Guid>> RemoveFlashDealItemsAsync(Guid flashDealId, CancellationToken ct)
    {
        var owners = await _dbContext.Database.SqlQuery<Guid>($"""
            DELETE FROM cart_items AS item
            USING flash_deal_variants AS variant
            WHERE item.flash_deal_variant_id = variant.id AND variant.flash_deal_id = {flashDealId}
            RETURNING item.user_id AS "Value"
            """).ToListAsync(ct);
        return owners.Distinct().ToList();
    }
}
