using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

// Read only historical payments owned by an active account, regardless of Customer feature status.
public sealed class PaymentHistoryRepository : IPaymentHistoryRepository
{
    private readonly AppDbContext _db;
    public PaymentHistoryRepository(AppDbContext db) { _db = db; }

    // Customer suspension blocks purchases, not access to payment records already owned by the user.
    public async Task RequireCustomerAsync(Guid userId, CancellationToken ct)
    {
        if (!await _db.Users.AsNoTracking().AnyAsync(
            x => x.Id == userId && x.Status == UserStatus.Active && x.Role != UserRole.Admin, ct))
            throw AppException.Forbidden("An active account is required to view payment history.");
    }

    private IQueryable<Payment> Payments => _db.Payments.AsNoTracking().Include(x => x.Order).ThenInclude(x => x.Shop)
        .Include(x => x.CheckoutSession);

    // Scope both rows and totals to the authenticated customer's orders before applying pagination.
    public async Task<(IReadOnlyList<Payment> Items, int TotalItems)> PageAsync(Guid userId, PaymentStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = Payments.Where(x => x.Order.CustomerId == userId || x.CheckoutSession!.CustomerId == userId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    // Read one payment only when it belongs to the authenticated account.
    public Task<Payment?> GetAsync(Guid userId, Guid paymentId, CancellationToken ct)
        => Payments.SingleOrDefaultAsync(x => x.Id == paymentId && (x.Order.CustomerId == userId || x.CheckoutSession!.CustomerId == userId), ct);
}
