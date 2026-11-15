using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.BackgroundJobs.Jobs;
namespace SaveBite.Backend.BackgroundJobs.Scheduling;

public sealed class PenaltyRuleScheduler(AppDbContext db, IBackgroundJobClient jobs, TimeProvider clock, ILogger<PenaltyRuleScheduler>? logger = null) : IPenaltyRuleScheduler
{
    public void Cancel(string? startId, string? endId)
    {
        foreach (var jobId in new[] { startId, endId }.Where(id => id != null))
        {
            try { jobs.Delete(jobId!); }
            catch (Exception ex) { logger?.LogWarning(ex, "Could not delete obsolete penalty rule job {JobId}; deadline validation rejects it.", jobId); }
        }
    }

    public async Task SyncAsync(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE recovery_requirement_rules IN SHARE ROW EXCLUSIVE MODE", ct);
        var rule = await db.RecoveryRequirementRules.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (rule == null) { await tx.CommitAsync(ct); return; }
        await db.Entry(rule).ReloadAsync(ct);
        Cancel(rule.StartJobId, rule.EndJobId);
        rule.StartJobId = rule.EndJobId = null;
        if (rule.IsActive)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            if (rule.EffectiveFrom <= now) rule.StartProcessedAt ??= now;
            else rule.StartJobId = SafeSchedule.Try(() => jobs.Schedule<PenaltyRuleBoundaryJob>(j => j.ExecuteAsync(id, rule.EffectiveFrom, false, CancellationToken.None), new DateTimeOffset(rule.EffectiveFrom)),
                logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PenaltyRuleScheduler>.Instance, "penalty-start", id);
            if (rule.EffectiveTo is DateTime until)
            {
                if (until <= now) rule.EndProcessedAt ??= now;
                else rule.EndJobId = SafeSchedule.Try(() => jobs.Schedule<PenaltyRuleBoundaryJob>(j => j.ExecuteAsync(id, until, true, CancellationToken.None), new DateTimeOffset(until)),
                    logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PenaltyRuleScheduler>.Instance, "penalty-end", id);
            }
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
