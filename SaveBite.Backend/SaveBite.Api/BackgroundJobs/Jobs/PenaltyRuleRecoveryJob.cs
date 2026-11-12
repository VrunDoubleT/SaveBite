using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.BackgroundJobs.Interfaces;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class PenaltyRuleRecoveryJob(AppDbContext db, IPenaltyRuleScheduler scheduler, TimeProvider clock)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var ids = await db.RecoveryRequirementRules.AsNoTracking().Where(r => r.IsActive &&
            ((r.StartProcessedAt == null && (r.EffectiveFrom <= now || r.StartJobId == null)) ||
             (r.EffectiveTo != null && r.EndProcessedAt == null && (r.EffectiveTo <= now || r.EndJobId == null))))
            .Select(r => r.Id).ToListAsync(ct);
        foreach (var id in ids) await scheduler.SyncAsync(id, ct);
    }
}
