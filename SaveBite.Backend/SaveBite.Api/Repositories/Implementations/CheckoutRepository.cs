using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

// Persist checkout, payment and stock changes with transaction and row-lock coordination.
public sealed class CheckoutRepository : ICheckoutRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ICartRepository _cartRepository;
    private readonly IRealtimePublisher? _realtimePublisher;

    public CheckoutRepository(AppDbContext db, ICartRepository cart, IRealtimePublisher? realtime = null)
    {
        _dbContext = db;
        _cartRepository = cart;
        _realtimePublisher = realtime;
    }

    // Load active customer access, shop purchase eligibility, cart items and the current deposit policy.
    public async Task<CheckoutState> GetStateAsync(Guid userId, Guid shopId, CancellationToken ct)
    {
        var user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw AppException.Unauthorized();
        if (user.Status != UserStatus.Active || user.CustomerStatus != CustomerStatus.Active || user.Role == UserRole.Admin)
            throw AppException.Forbidden("Active Customer access is required to checkout, including Owner and Staff accounts.");
        // Recheck at context, quote and placement so old cart entries or quotes cannot bypass shop membership changes.
        if (await _cartRepository.IsShopOperatorAsync(userId, shopId, ct))
            throw AppException.Forbidden("You cannot purchase items from a shop you own or actively work for.");
        var score = await _dbContext.UserTrustScores.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var levels = _dbContext.TrustLevels.AsNoTracking().Include(x => x.DepositPolicy)!.ThenInclude(x => x!.Ranges);
        var level = score == null
            ? await levels.Where(x => x.IsActive).OrderBy(x => x.Rank).FirstOrDefaultAsync(ct)
            : await levels.SingleOrDefaultAsync(x => x.Id == score.TrustLevelId, ct);
        var permanent = score?.IsOfflineLockedPermanently == true || await _dbContext.NoShowPenalties.AsNoTracking().AnyAsync(x => x.UserId == userId && x.PermanentOfflineLock, ct);
        var recovering = await _dbContext.NoShowPenalties.AsNoTracking().AnyAsync(x => x.UserId == userId && x.Status == NoShowPenaltyStatus.Recovering, ct);
        return new(user, await _cartRepository.GetItemsAsync(userId, shopId, ct), level, score == null, permanent, recovering,
            await _dbContext.Set<ShopPaymentConfig>().AsNoTracking().SingleOrDefaultAsync(x => x.ShopId == shopId, ct));
    }

    private IQueryable<Order> Orders => _dbContext.Orders.Include(x => x.Items).ThenInclude(x => x.FlashDealVariant)
        .ThenInclude(x => x.FlashDeal).ThenInclude(x => x.Product).ThenInclude(x => x.Images).Include(x => x.Payments)
        .Include(x => x.Items).ThenInclude(x => x.Feedback)
        .Include(x => x.Shop).ThenInclude(x => x.PaymentConfig).AsSplitQuery();
    // Find a previous checkout by request or quote ID to prevent duplicate orders.
    public async Task<Order?> FindRequestAsync(Guid userId, Guid requestId, Guid quoteId, CancellationToken ct)
    {
        var matches = await Orders.Where(x => x.CustomerId == userId && (x.CheckoutRequestId == requestId || x.CheckoutQuoteId == quoteId)).Take(2).ToListAsync(ct);
        if (matches.Count > 1) throw AppException.Conflict("This request ID belongs to a different checkout.");
        return matches.SingleOrDefault();
    }
    // Reject order access unless this account has active Customer access.
    private async Task RequireActiveCustomerAsync(Guid userId, CancellationToken ct)
    {
        if (!await _dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.Status == UserStatus.Active
            && x.CustomerStatus == CustomerStatus.Active && x.Role != UserRole.Admin, ct))
            throw AppException.Forbidden("Active Customer access is required to access orders.");
    }
    // Six-digit strings sort numerically; legacy long codes remain unchanged and are excluded from the counter floor.
    public async Task<int> OrderCodeMaximumAsync(Guid shopId, DateOnly date, CancellationToken ct)
    {
        var maximum = await _dbContext.Orders.AsNoTracking().Where(x => x.ShopId == shopId && x.OrderDate == date
            && System.Text.RegularExpressions.Regex.IsMatch(x.OrderCode, "^[0-9]{6}$"))
            .OrderByDescending(x => x.OrderCode).Select(x => x.OrderCode).FirstOrDefaultAsync(ct);
        return int.TryParse(maximum, out var value) ? value : 0;
    }

    // Load recorded order and payment data, optionally restricted to its customer.
    public async Task<Order?> GetOrderAsync(Guid orderId, Guid? userId, CancellationToken ct)
    {
        if (userId.HasValue) await RequireActiveCustomerAsync(userId.Value, ct);
        return await Orders.SingleOrDefaultAsync(x => x.Id == orderId && (!userId.HasValue || x.CustomerId == userId), ct);
    }
    // Read the customer's most recent orders after checking active Customer access.
    public async Task<IReadOnlyList<Order>> GetOrdersAsync(Guid userId, CancellationToken ct)
    {
        await RequireActiveCustomerAsync(userId, ct);
        return await Orders.AsNoTracking().Where(x => x.CustomerId == userId).OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);
    }
    // Read a customer-scoped cursor page with status counts for the order filters.
    public async Task<(IReadOnlyList<Order> Orders, bool HasMore, IReadOnlyDictionary<string, int> Counts)> GetOrderPageAsync(Guid userId, OrderStatus? status, DateTime? beforeCreatedAt, Guid? beforeId, int pageSize, CancellationToken ct)
    {
        await RequireActiveCustomerAsync(userId, ct);
        var query = Orders.AsNoTracking().Where(x => x.CustomerId == userId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (beforeCreatedAt.HasValue && beforeId.HasValue)
            query = query.Where(x => x.CreatedAt < beforeCreatedAt.Value || (x.CreatedAt == beforeCreatedAt.Value && x.Id.CompareTo(beforeId.Value) < 0));
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(pageSize + 1).ToListAsync(ct);
        var counts = await _dbContext.Orders.AsNoTracking().Where(x => x.CustomerId == userId).GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() }).ToListAsync(ct);
        return (rows.Take(pageSize).ToList(), rows.Count > pageSize, counts.ToDictionary(x => x.Status.ToString(), x => x.Count));
    }
    // Resolve a provider payment code to its associated order for webhook handling.
    public Task<Guid?> FindGatewayOrderAsync(long gatewayCode, CancellationToken ct)
        => _dbContext.Payments.AsNoTracking().Where(x => x.GatewayOrderCode == gatewayCode).Select(x => (Guid?)x.OrderId).SingleOrDefaultAsync(ct);
    // Track a new order so it is saved with the current checkout transaction.
    public void AddOrder(Order order) => _dbContext.Orders.Add(order);
    // Track an order history entry in the same transaction as the state change.
    public void AddHistory(OrderStatusHistory history) => _dbContext.OrderStatusHistories.Add(history);
    // Atomically reserve each variant only when enough stock remains available.
    public async Task ReserveAsync(IReadOnlyList<CartItem> items, CancellationToken ct)
    {
        foreach (var item in items.OrderBy(x => x.FlashDealVariantId))
        {
            var changed = await _dbContext.FlashDealVariants.Where(x => x.Id == item.FlashDealVariantId
                    && x.TotalQuantity - x.ReservedQuantity - x.SoldQuantity >= item.Quantity)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.ReservedQuantity, x => x.ReservedQuantity + item.Quantity), ct);
            if (changed != 1) throw AppException.OutOfStock("Stock changed. Please refresh checkout.");
        }
    }
    // Settle an active reservation once by releasing it or moving its quantity to sold stock.
    public async Task CompleteInventoryAsync(Order order, bool sold, CancellationToken ct)
    {
        if (!order.InventoryReservationActive) return;
        foreach (var item in order.Items.OrderBy(x => x.FlashDealVariantId))
        {
            var changed = await _dbContext.FlashDealVariants.Where(x => x.Id == item.FlashDealVariantId && x.ReservedQuantity >= item.Quantity)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(x => x.ReservedQuantity, x => x.ReservedQuantity - item.Quantity)
                    .SetProperty(x => x.SoldQuantity, x => x.SoldQuantity + (sold ? item.Quantity : 0)), ct);
            if (changed != 1) throw new InvalidOperationException("Order inventory could not be reconciled; the transaction was rolled back.");
        }
        order.InventoryReservationActive = false;
    }
    // Return eligible reserved or sold quantities when a customer cancels before pickup.
    public async Task ReleaseCancelledInventoryAsync(Order order, CancellationToken ct)
    {
        if (order.Status == OrderStatus.Cancelled) return;
        if (order.InventoryReservationActive)
        {
            await CompleteInventoryAsync(order, false, ct);
            return;
        }
        // Checkout moves reserved units to sold on verified payment (including zero-deposit orders).
        // A pre-pickup customer cancellation makes those units available again exactly once.
        if (order.CheckoutQuoteId == null
            || !order.Payments.Any(x => (x.PaymentType == PaymentType.Full || x.PaymentType == PaymentType.Deposit) && x.Status == PaymentStatus.Succeeded)) return;
        foreach (var item in order.Items.OrderBy(x => x.FlashDealVariantId))
        {
            var changed = await _dbContext.FlashDealVariants.Where(x => x.Id == item.FlashDealVariantId && x.SoldQuantity >= item.Quantity)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.SoldQuantity, x => x.SoldQuantity - item.Quantity), ct);
            if (changed != 1) throw new InvalidOperationException("Cancelled inventory could not be released; the transaction was rolled back.");
        }
    }
    private bool cartChanged;
    // Remove only the purchased shop's cart items and remember that a cart event is needed.
    public async Task RemoveShopCartAsync(Guid userId, Guid shopId, CancellationToken ct)
        => cartChanged |= await _dbContext.CartItems.Where(x => x.UserId == userId && x.FlashDealVariant.FlashDeal.ShopId == shopId).ExecuteDeleteAsync(ct) > 0;
    // Persist tracked checkout, payment and history changes inside the current transaction.
    public async Task SaveAsync(CancellationToken ct) => await _dbContext.SaveChangesAsync(ct);

    // Lock the customer and checkout dependencies before creating an order and committing cart changes.
    public async Task<T> WithUserLockAsync<T>(Guid userId, Guid shopId, Func<Task<T>> action, CancellationToken ct)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM users WHERE id = {userId} FOR UPDATE", ct);
        // Lock parents before stock rows in a stable order. Status/price/policy cannot change during commit.
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM shops WHERE id = {shopId} FOR SHARE", ct);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT d.id FROM flash_deals d JOIN flash_deal_variants v ON v.flash_deal_id = d.id JOIN cart_items c ON c.flash_deal_variant_id = v.id WHERE c.user_id = {userId} AND d.shop_id = {shopId} ORDER BY d.id FOR SHARE OF d", ct);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT p.id FROM \"Products\" p JOIN flash_deals d ON d.product_id = p.id JOIN flash_deal_variants v ON v.flash_deal_id = d.id JOIN cart_items c ON c.flash_deal_variant_id = v.id WHERE c.user_id = {userId} AND d.shop_id = {shopId} ORDER BY p.id FOR SHARE OF p", ct);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT p.id FROM product_variants p JOIN flash_deal_variants v ON v.variant_id = p.id JOIN flash_deals d ON d.id = v.flash_deal_id JOIN cart_items c ON c.flash_deal_variant_id = v.id WHERE c.user_id = {userId} AND d.shop_id = {shopId} ORDER BY p.id FOR SHARE OF p", ct);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT v.id FROM flash_deal_variants v JOIN cart_items c ON c.flash_deal_variant_id = v.id JOIN flash_deals d ON d.id = v.flash_deal_id WHERE c.user_id = {userId} AND d.shop_id = {shopId} ORDER BY v.id FOR UPDATE OF v", ct);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM user_trust_scores WHERE user_id = {userId} FOR SHARE", ct);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM no_show_penalties WHERE user_id = {userId} FOR SHARE", ct);
        await _dbContext.Database.ExecuteSqlRawAsync("SELECT id FROM trust_levels ORDER BY id FOR SHARE", ct);
        await _dbContext.Database.ExecuteSqlRawAsync("SELECT id FROM trust_level_deposit_policies ORDER BY id FOR SHARE", ct);
        await _dbContext.Database.ExecuteSqlRawAsync("SELECT id FROM trust_level_deposit_ranges ORDER BY id FOR SHARE", ct);
        // A unique-code retry must reread cart/order state instead of reusing entities from a rolled-back attempt.
        _dbContext.ChangeTracker.Clear();
        cartChanged = false;
        var result = await action();
        await tx.CommitAsync(ct);
        if (cartChanged && _realtimePublisher != null)
            await _realtimePublisher.SendToUserAsync(userId, RealtimeEvents.CartChanged, new CartChangedEvent(RealtimeEvents.CartReasons.Checkout));
        cartChanged = false;
        return result;
    }
    // Serialize order changes under customer and order row locks before committing the result.
    public async Task<T> WithOrderLockAsync<T>(Guid orderId, Func<Task<T>> action, CancellationToken ct)
    {
        var ownerId = await _dbContext.Orders.AsNoTracking().Where(x => x.Id == orderId).Select(x => (Guid?)x.CustomerId).SingleOrDefaultAsync(ct);
        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        // Same lock order as cart/checkout: customer -> order -> stock. Cancellation's FK to users must not deadlock a new checkout.
        if (ownerId.HasValue)
            await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM users WHERE id = {ownerId.Value} FOR UPDATE", ct);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM orders WHERE id = {orderId} FOR UPDATE", ct);
        _dbContext.ChangeTracker.Clear();
        var result = await action();
        await tx.CommitAsync(ct);
        return result;
    }
}
