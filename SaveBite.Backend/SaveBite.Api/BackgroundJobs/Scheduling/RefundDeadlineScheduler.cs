using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.BackgroundJobs.Scheduling;

public sealed class RefundDeadlineScheduler(IBackgroundJobClient jobs, ILogger<RefundDeadlineScheduler> logger) : IRefundDeadlineScheduler
{
    public string? Schedule(Guid refundId, DateTime deadline)
        => SafeSchedule.Try(() => jobs.Schedule<RefundDeadlineJob>(job => job.ExecuteAsync(refundId, deadline, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(deadline, DateTimeKind.Utc))), logger, "refund-deadline", refundId);

    public bool Delete(string jobId)
    {
        try { return jobs.Delete(jobId); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not delete job {JobId}.", jobId); return false; }
    }
}
