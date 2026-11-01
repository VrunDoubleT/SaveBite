using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
namespace SaveBite.Backend.Repositories.Implementations;

public sealed class TrustNotificationRepository(AppDbContext db) : ITrustNotificationRepository
{
    public Task<List<TrustScoreHistory>> ScoreChangesAsync(Guid orderId, CancellationToken ct)
        => db.TrustScoreHistories.AsNoTracking().Where(h => h.OrderId == orderId).ToListAsync(ct);
    public Task<List<NoShowPenalty>> PenaltiesAsync(Guid orderId, CancellationToken ct)
        => db.NoShowPenalties.AsNoTracking().Where(p => p.OrderId == orderId).ToListAsync(ct);
    public Task<List<UserTrustLevelHistory>> LevelChangesAsync(Guid orderId, CancellationToken ct)
        => db.UserTrustLevelHistories.AsNoTracking().Include(h => h.NewTrustLevel).Where(h => h.OrderId == orderId).ToListAsync(ct);
}
