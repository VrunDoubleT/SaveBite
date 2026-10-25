using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
namespace SaveBite.Backend.Repositories.Implementations;

public sealed class TrustOrderRepository : ITrustOrderRepository
{
    private readonly AppDbContext _db;
    public TrustOrderRepository(AppDbContext db) { _db = db; }

    // Serialize terminal-order processing by customer, then commit progress, histories and receipt together.
    public async Task ExecuteAsync(Guid orderId, Func<Order, Task> handle, CancellationToken ct)
    {
        var userId = await _db.Orders.AsNoTracking().Where(x => x.Id == orderId).Select(x => (Guid?)x.CustomerId).SingleOrDefaultAsync(ct);
        if (!userId.HasValue) throw new InvalidOperationException("Order not found for trust processing.");
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM users WHERE id = {userId.Value} FOR UPDATE", ct);
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM orders WHERE id = {orderId} FOR SHARE", ct);
        _db.ChangeTracker.Clear();
        var order = await _db.Orders.Include(x => x.StatusHistories).SingleAsync(x => x.Id == orderId, ct);
        await handle(order);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    // Read the idempotency receipt independently of whether an earning or penalty rule existed.
    public async Task<bool> IsProcessedAsync(Guid orderId, CancellationToken ct)
        => await _db.Set<TrustOrderProcessing>().AnyAsync(x => x.OrderId == orderId, ct)
            || await _db.TrustScoreHistories.AnyAsync(x => x.OrderId == orderId, ct)
            || await _db.NoShowPenalties.AnyAsync(x => x.OrderId == orderId, ct);
    public Task<UserTrustScore?> ScoreAsync(Guid userId, CancellationToken ct)
        => _db.UserTrustScores.SingleOrDefaultAsync(x => x.UserId == userId, ct);
    public Task<UserLevelProgress?> ProgressAsync(Guid userId, CancellationToken ct)
        => _db.UserLevelProgresses.SingleOrDefaultAsync(x => x.UserId == userId, ct);
    public Task<List<TrustLevel>> LevelsAsync(CancellationToken ct)
        => _db.TrustLevels.Include(x => x.Requirement).Where(x => x.IsActive).OrderBy(x => x.Rank).ToListAsync(ct);
    // Use the order event timestamp, not delayed consumer execution time, to resolve rule versions.
    public Task<TrustScoreRule?> EarningAsync(TrustScoreRuleType type, DateTime at, CancellationToken ct)
        => _db.TrustScoreRules.Where(x => x.RuleType == type && x.ScoreDelta > 0 && x.EffectiveFrom <= at
            && (x.EffectiveTo == null || at < x.EffectiveTo)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
    public Task<int> OccurrencesAsync(Guid userId, int year, int month, CancellationToken ct)
        => _db.NoShowPenalties.CountAsync(x => x.UserId == userId && x.PenaltyYear == year && x.PenaltyMonth == month, ct);
    public Task<RecoveryRequirementRule?> PenaltyRuleAsync(int occurrence, DateTime at, CancellationToken ct)
        => _db.RecoveryRequirementRules.Where(x => x.IsActive && x.MinimumOccurrence <= occurrence && x.EffectiveFrom <= at
            && (x.EffectiveTo == null || at < x.EffectiveTo)).OrderByDescending(x => x.MinimumOccurrence)
            .ThenByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
    public Task<List<NoShowPenalty>> RecoveringAsync(Guid userId, CancellationToken ct)
        => _db.NoShowPenalties.Where(x => x.UserId == userId && x.Status == NoShowPenaltyStatus.Recovering)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(ct);
    // One full-online completed order can recover one penalty; recorded assignments prevent reuse.
    public Task<List<Order>> RecoveryCandidatesAsync(Guid userId, CancellationToken ct)
        => _db.Orders.AsNoTracking().Where(x => x.CustomerId == userId && x.Status == OrderStatus.Completed
            && x.PaymentMethod == PaymentMethod.OnlinePayment && x.CompletedAt != null
            && !_db.NoShowPenaltyOrderHistories.Any(h => h.OrderId == x.Id))
            .OrderBy(x => x.CompletedAt).ThenBy(x => x.Id).ToListAsync(ct);
    public void Add<T>(T entity) where T : class => _db.Set<T>().Add(entity);
}
