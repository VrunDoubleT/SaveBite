using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.BackgroundJobs.Interfaces;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class PenaltyRuleRecoveryJob(IServiceScopeFactory scopes, TimeProvider clock, ILogger<PenaltyRuleRecoveryJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await RecoveryBatch.RunAsync(scopes, logger, "penalty-rule", db => db.RecoveryRequirementRules.AsNoTracking().Where(r => r.IsActive &&
            ((r.StartProcessedAt == null && (r.EffectiveFrom <= now || r.StartJobId == null)) ||
             (r.EffectiveTo != null && r.EndProcessedAt == null && (r.EffectiveTo <= now || r.EndJobId == null))))
            .Select(r => r.Id), (sp, id) => sp.GetRequiredService<IPenaltyRuleScheduler>().SyncAsync(id, ct), ct);
    }
}
