using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Lifecycle markers do not toggle IsActive: delayed order events must still resolve past rule versions.
public sealed class PenaltyRuleBoundaryJob(AppDbContext db, TimeProvider clock)
{
    public async Task ExecuteAsync(Guid id, DateTime expectedAt, bool ending, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE recovery_requirement_rules IN SHARE ROW EXCLUSIVE MODE", ct);
        var rule = await db.RecoveryRequirementRules.SingleOrDefaultAsync(r => r.Id == id, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        if (rule != null && rule.IsActive && now >= expectedAt && (ending ? rule.EffectiveTo == expectedAt : rule.EffectiveFrom == expectedAt))
        {
            if (ending) { rule.EndProcessedAt ??= now; rule.EndJobId = null; }
            else { rule.StartProcessedAt ??= now; rule.StartJobId = null; }
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
}
