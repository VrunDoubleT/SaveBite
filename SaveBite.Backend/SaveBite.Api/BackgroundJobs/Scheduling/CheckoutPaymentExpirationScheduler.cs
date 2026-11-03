using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.BackgroundJobs.Scheduling;

public sealed class CheckoutPaymentExpirationScheduler(IBackgroundJobClient jobs, ILogger<CheckoutPaymentExpirationScheduler> logger) : ICheckoutPaymentExpirationScheduler
{
    public string? Schedule(Guid orderId, DateTime expiresAt)
        => SafeSchedule.Try(() => jobs.Schedule<CheckoutPaymentExpirationJob>(job => job.ExecuteAsync(orderId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc))), logger, "payment-expiration", orderId);

    public bool Delete(string jobId)
    {
        try { return jobs.Delete(jobId); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not delete job {JobId}.", jobId); return false; }
    }
}
