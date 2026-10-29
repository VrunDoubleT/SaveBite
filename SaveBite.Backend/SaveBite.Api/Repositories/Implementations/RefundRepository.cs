using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
namespace SaveBite.Backend.Repositories.Implementations;

public class RefundRepository : IRefundRepository
{
    private readonly AppDbContext _db;
    public RefundRepository(AppDbContext db) { _db = db; }

    private IQueryable<Refund> Refunds => _db.Refunds.Include(x => x.Order).ThenInclude(x => x.Shop)
        .Include(x => x.Order).ThenInclude(x => x.Customer).ThenInclude(x => x.TrustScore).ThenInclude(x => x!.TrustLevel).Include(x => x.History).Include(x => x.EvidenceImages).AsSplitQuery();

    // Count active shop obligations, including those waiting for the customer, but exclude closed requests.
    public Task<int> ShopPendingCountAsync(Guid shopId, CancellationToken ct)
        => _db.Refunds.AsNoTracking().CountAsync(x => x.Order.ShopId == shopId
            && (x.Status == RefundStatus.AwaitingCustomerDetails || x.Status == RefundStatus.PendingVerification || x.Status == RefundStatus.AwaitingTransfer || x.Status == RefundStatus.Overdue)
            && (x.RefundType == RefundType.ShopCancellation || (x.Order.Status == OrderStatus.Completed
                && x.DestinationConfirmedAt != null && x.Status != RefundStatus.AwaitingCustomerDetails)), ct);

    // Count only mandatory shop refunds that still need customer bank/email details.
    public Task<int> PendingDetailsCountAsync(Guid userId, CancellationToken ct)
        => _db.Refunds.AsNoTracking().CountAsync(x => x.Order.CustomerId == userId
            && x.RefundType == RefundType.ShopCancellation && x.Status == RefundStatus.AwaitingCustomerDetails
            && x.DestinationConfirmedAt == null, ct);

    // View queries disable tracking and never perform writes or payment reconciliation.
    public Task<Refund?> GetAsync(Guid id, bool tracked, CancellationToken ct)
        => (tracked ? Refunds : Refunds.AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<Refund>> ForOrderAsync(Guid orderId, CancellationToken ct)
        => Refunds.AsNoTracking().Where(x => x.OrderId == orderId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToListAsync(ct);
    public Task<Order?> OrderAsync(Guid id, CancellationToken ct)
        => _db.Orders.Include(x => x.Customer).ThenInclude(x => x.TrustScore).ThenInclude(x => x!.TrustLevel).Include(x => x.Shop).Include(x => x.Payments)
            .Include(x => x.Refunds).ThenInclude(x => x.History).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, ct);

    // Scope before filtering or counting, including the shop's visibility gate for customer requests.
    public async Task<(List<Refund> Items, int Total)> PageAsync(Guid userId, Guid? shopId, RefundStatus? status, RefundType? type, int page, int size, CancellationToken ct)
    {
        var query = Refunds.AsNoTracking();
        query = shopId.HasValue ? query.Where(x => x.Order.ShopId == shopId.Value
            && (x.RefundType == RefundType.ShopCancellation || (x.Order.Status == OrderStatus.Completed && x.DestinationConfirmedAt != null)))
            : query.Where(x => x.Order.CustomerId == userId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (type.HasValue) query = query.Where(x => x.RefundType == type.Value);
        var total = await query.CountAsync(ct);
        return (await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct), total);
    }
    public Task<Guid?> SystemRefundIdAsync(Guid orderId, CancellationToken ct)
        => _db.Refunds.AsNoTracking().Where(x => x.OrderId == orderId && x.RefundType == RefundType.ShopCancellation)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
    public async Task<User> UserAsync(Guid id, CancellationToken ct)
        => await _db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw AppException.Unauthorized();
    public Task<TrustOrderProcessing?> ProcessingAsync(Guid orderId, CancellationToken ct)
        => _db.Set<TrustOrderProcessing>().SingleOrDefaultAsync(x => x.OrderId == orderId, ct);
    public Task<bool> LegacyCreditAsync(Guid orderId, CancellationToken ct)
        => _db.TrustScoreHistories.AnyAsync(x => x.OrderId == orderId && x.EventType == TrustScoreEventType.OrderCompleted, ct);
    public Task<OrderPlatformFee?> FeeAsync(Guid orderId, CancellationToken ct)
        => _db.Set<OrderPlatformFee>().Include(x => x.Adjustments).SingleOrDefaultAsync(x => x.OrderId == orderId, ct);
    public void Add<T>(T entity) where T : class => _db.Set<T>().Add(entity);
}
