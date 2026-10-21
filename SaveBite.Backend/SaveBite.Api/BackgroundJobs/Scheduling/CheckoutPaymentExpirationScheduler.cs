using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.BackgroundJobs.Scheduling;

// Manage Hangfire jobs for initial payment expiry.
public sealed class CheckoutPaymentExpirationScheduler : ICheckoutPaymentExpirationScheduler
{
    private readonly IBackgroundJobClient _jobs;

    public CheckoutPaymentExpirationScheduler(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    // Persist one delayed job for this order instead of scanning pending payments.
    public string Schedule(Guid orderId, DateTime expiresAt)
    {
        var jobId = _jobs.Schedule<CheckoutPaymentExpirationJob>(
            job => job.ExecuteAsync(orderId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)));
        if (string.IsNullOrWhiteSpace(jobId))
            throw new InvalidOperationException("Payment expiration could not be scheduled.");
        return jobId;
    }
    // Deletion is best effort; an already-running job still validates order and payment state.
    public bool Delete(string jobId) => _jobs.Delete(jobId);
}
