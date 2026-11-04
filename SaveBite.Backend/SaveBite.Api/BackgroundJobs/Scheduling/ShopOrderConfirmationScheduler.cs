using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.BackgroundJobs.Scheduling;

public sealed class ShopOrderConfirmationScheduler(IBackgroundJobClient jobs, ILogger<ShopOrderConfirmationScheduler> logger) : IShopOrderConfirmationScheduler
{
    public string? Schedule(Guid orderId, DateTime deadline)
        => SafeSchedule.Try(() => jobs.Schedule<ShopOrderConfirmationJob>(job => job.ExecuteAsync(orderId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(deadline, DateTimeKind.Utc))), logger, "confirmation-expiration", orderId);

    public bool Delete(string jobId)
    {
        try { return jobs.Delete(jobId); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not delete job {JobId}.", jobId); return false; }
    }
}
