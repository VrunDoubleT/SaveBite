using SaveBite.Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

// Read shop orders and persist payment, refund and staff audit records.
public sealed class ShopOrderRepository : IShopOrderRepository
{
    private readonly AppDbContext _db;

    public ShopOrderRepository(
        AppDbContext db)
    {
        _db = db;
    }

    // Allow only this shop's active owner or active staff and indicate whether the actor is the owner.
    public async Task<bool> RequireAccessAsync(Guid userId, Guid shopId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user == null || user.Status != UserStatus.Active || user.Role == UserRole.Admin)
            throw AppException.Forbidden("Active shop access is required.");
        var shop = await _db.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.Id == shopId, ct);
        if (shop == null || shop.Status != ShopStatus.Active) throw AppException.Forbidden("This shop is not active.");
        if (shop.OwnerUserId == userId && user.ShopStatus == ShopAccessStatus.Active) return true;
        if (await _db.ShopStaffMembers.AsNoTracking().AnyAsync(x => x.ShopId == shopId && x.UserId == userId && x.Status == ShopStaffStatus.Active, ct))
            return false;
        throw AppException.Forbidden("Only the shop owner or an active staff member may access these orders.");
    }
    // Resolve the operator's ID, shop role and display name for historical audit snapshots.
    public async Task<SaveBite.Backend.Models.DTOs.ShopOrderActor> ActorAsync(Guid userId, Guid shopId, CancellationToken ct)
    {
        var owner = await RequireAccessAsync(userId, shopId, ct);
        var user = await _db.Users.AsNoTracking().SingleAsync(x => x.Id == userId, ct);
        var name = owner ? user.FullName : await _db.ShopStaffMembers.AsNoTracking()
            .Where(x => x.ShopId == shopId && x.UserId == userId && x.Status == ShopStaffStatus.Active)
            .Select(x => x.DisplayName).SingleAsync(ct);
        return new(userId, owner ? "StoreOwner" : "Staff", string.IsNullOrWhiteSpace(name) ? user.FullName : name.Trim());
    }
    // Find the active shop owned by the authenticated account.
    public async Task<Guid> OwnerShopAsync(Guid userId, CancellationToken ct)
    {
        var id = await _db.Shops.AsNoTracking().Where(x => x.OwnerUserId == userId).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Your shop was not found.");
        if (!await RequireAccessAsync(userId, id, ct)) throw AppException.Forbidden();
        return id;
    }
    // Count submitted Pending orders without loading items, payments or history.
    public Task<int> PendingCountAsync(Guid shopId, CancellationToken ct)
        => ShopOrderVisibility.Apply(_db.Orders.AsNoTracking())
            .CountAsync(x => x.ShopId == shopId && x.Status == OrderStatus.Pending, ct);

    private IQueryable<Order> Orders => _db.Orders.Include(x => x.Customer).Include(x => x.Shop).ThenInclude(x => x.PaymentConfig)
        .Include(x => x.Items).ThenInclude(x => x.FlashDealVariant).ThenInclude(x => x.FlashDeal).ThenInclude(x => x.Product).ThenInclude(x => x.Images)
        .Include(x => x.Shop).ThenInclude(x => x.StaffMembers)
        .Include(x => x.Payments).Include(x => x.Refunds).Include(x => x.StatusHistories).ThenInclude(x => x.ChangedByUser).AsSplitQuery();
    // Load a shop-scoped order with recorded items, payments, customer and audit history.
    public Task<Order?> GetAsync(Guid shopId, Guid orderId, CancellationToken ct)
        => Orders.SingleOrDefaultAsync(x => x.ShopId == shopId && x.Id == orderId, ct);
    // Read a numbered shop-order page with status counts and optional exact code or ID search.
    public async Task<(IReadOnlyList<Order> Items, int TotalItems, IReadOnlyDictionary<string, int> Counts)> PageAsync(
        Guid shopId, string? status, string? search, int page, int size, CancellationToken ct, DateOnly? orderDate = null)
    {
        var all = ShopOrderVisibility.Apply(_db.Orders.AsNoTracking()).Where(x => x.ShopId == shopId);
        var groups = await all.GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() }).ToListAsync(ct);
        var counts = groups.ToDictionary(x => x.Status.ToString(), x => x.Count);
        counts["All"] = counts.Values.Sum();
        var query = ShopOrderVisibility.Apply(Orders.AsNoTracking()).Where(x => x.ShopId == shopId);
        if (status != null) { var parsed = Enum.Parse<OrderStatus>(status); query = query.Where(x => x.Status == parsed); }
        if (search != null)
        {
            if (Guid.TryParse(search, out var id)) query = query.Where(x => x.Id == id);
            else
            {
                query = query.Where(x => x.OrderCode.ToUpper() == search.ToUpper());
                if (search.Length == 6 && search.All(char.IsAsciiDigit) && orderDate.HasValue)
                    query = query.Where(x => x.OrderDate == orderDate.Value);
            }
        }
        var totalItems = await query.CountAsync(ct);
        // UUID is only a deterministic tie-breaker for orders created at the same time.
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return (rows, totalItems, counts);
    }
    // Customer order events belong only to the active customer who owns this shop's order.
    public Task<Guid?> CustomerRecipientAsync(Guid shopId, Guid orderId, CancellationToken ct)
        => _db.Orders.AsNoTracking().Where(x => x.Id == orderId && x.ShopId == shopId
            && x.Customer.Status == UserStatus.Active && x.Customer.CustomerStatus == CustomerStatus.Active
            && x.Customer.Role != UserRole.Admin)
            .Select(x => (Guid?)x.CustomerId).SingleOrDefaultAsync(ct);

    // Find the shop's active owner and staff accounts eligible for private realtime events.
    public async Task<IReadOnlyList<Guid>> RealtimeRecipientsAsync(Guid shopId, CancellationToken ct)
        => await _db.Users.AsNoTracking().Where(user => user.Status == UserStatus.Active && user.Role != UserRole.Admin
            && (_db.Shops.Any(shop => shop.Id == shopId && shop.Status == ShopStatus.Active && shop.OwnerUserId == user.Id && user.ShopStatus == ShopAccessStatus.Active)
                || _db.ShopStaffMembers.Any(staff => staff.ShopId == shopId && staff.UserId == user.Id && staff.Status == ShopStaffStatus.Active
                    && staff.Shop.Status == ShopStatus.Active)))
            .Select(user => user.Id).ToListAsync(ct);

    // Track a new payment receipt for the current order transaction.
    public void AddPayment(Payment payment) => _db.Payments.Add(payment);
    // Track a refund request without initiating a money transfer.
    public Task<OrderPlatformFee?> FeeAsync(Guid orderId, CancellationToken ct)
        => _db.Set<OrderPlatformFee>().Include(x => x.Adjustments).SingleOrDefaultAsync(x => x.OrderId == orderId, ct);
    public void AddFeeAdjustment(PlatformFeeAdjustment adjustment) => _db.Set<PlatformFeeAdjustment>().Add(adjustment);
    public void AddRefundHistory(RefundStatusHistory history) => _db.Set<RefundStatusHistory>().Add(history);
    public void AddRefund(Refund refund) => _db.Set<Refund>().Add(refund);
    // Track a staff action with the operator identity captured at execution time.
    public void AddActivity(StaffActivityLog activity) => _db.Set<StaffActivityLog>().Add(activity);
}
