using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
namespace SaveBite.Backend.Repositories.Implementations;

public sealed class TrustNotificationRepository(AppDbContext db) : ITrustNotificationRepository
{
    public Task<List<NoShowPenaltyOrderHistory>> RecoveryChangesAsync(Guid orderId, CancellationToken ct)
        => db.NoShowPenaltyOrderHistories.AsNoTracking().Include(h => h.NoShowPenalty).Include(h => h.Order)
            .Where(h => h.OrderId == orderId || h.NoShowPenalty.OrderId == orderId).ToListAsync(ct);
    public Task<List<TrustProgressHistory>> ScoreChangesAsync(Guid orderId, CancellationToken ct)
        => db.TrustProgressHistories.AsNoTracking().Where(h => h.OrderId == orderId).ToListAsync(ct);
    public Task<List<NoShowPenalty>> PenaltiesAsync(Guid orderId, CancellationToken ct)
        => db.NoShowPenalties.AsNoTracking().Where(p => p.OrderId == orderId).ToListAsync(ct);
    public Task<List<UserTrustLevelHistory>> LevelChangesAsync(Guid orderId, CancellationToken ct)
        => db.UserTrustLevelHistories.AsNoTracking().Include(h => h.NewTrustLevel).Where(h => h.OrderId == orderId).ToListAsync(ct);
}
