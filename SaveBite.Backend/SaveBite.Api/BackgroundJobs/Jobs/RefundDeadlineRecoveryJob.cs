using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class RefundDeadlineRecoveryJob(IServiceScopeFactory scopes, TimeProvider clock, ILogger<RefundDeadlineRecoveryJob> logger)
{
    [DisableConcurrentExecution(600)]
    public Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return RecoveryBatch.RunAsync(scopes, logger, "refund-deadline", db => db.Refunds.AsNoTracking()
            .Where(f => (f.Status == RefundStatus.AwaitingCustomerDetails
                    && (f.DeadlineJobId == null || (f.CustomerDetailsDeadlineAt ?? f.CreatedAt.AddHours(24)) <= now))
                || ((f.Status == RefundStatus.PendingVerification || f.Status == RefundStatus.AwaitingTransfer)
                    && f.DestinationConfirmedAt != null
                    && (f.DeadlineJobId == null || (f.ReviewDeadlineAt ?? f.DestinationConfirmedAt.Value.AddHours(24)) <= now)))
            .Select(f => f.Id), async (sp, id) =>
            {
                try { await sp.GetRequiredService<IRefundService>().EnsureDeadlineScheduledAsync(id, ct); }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                { logger.LogWarning(ex, "Refund {Id} schedule/notification repair failed; checking expiry independently.", id); }
                var refund = await sp.GetRequiredService<AppDbContext>().Refunds.AsNoTracking().SingleOrDefaultAsync(f => f.Id == id, ct);
                if (refund == null || !RefundRules.Active(refund.Status)) return;
                if (RefundRules.Deadline(refund) is DateTime deadline && deadline <= clock.GetUtcNow().UtcDateTime)
                    await sp.GetRequiredService<RefundDeadlineJob>().ExecuteAsync(id, deadline, ct);
            }, ct);
    }
}
