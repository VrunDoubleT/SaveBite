using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.BackgroundJobs.Scheduling;

// Manage Hangfire jobs for overdue shop confirmation.
public sealed class ShopOrderConfirmationScheduler : IShopOrderConfirmationScheduler
{
    private readonly IBackgroundJobClient _jobs;
    public ShopOrderConfirmationScheduler(IBackgroundJobClient jobs) { _jobs = jobs; }

    // Schedule the confirmation timeout at the recorded absolute deadline.
    public string Schedule(Guid orderId, DateTime deadline)
    {
        var id = _jobs.Schedule<ShopOrderConfirmationJob>(job => job.ExecuteAsync(orderId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(deadline, DateTimeKind.Utc)));
        return !string.IsNullOrWhiteSpace(id) ? id : throw new InvalidOperationException("Shop confirmation expiry could not be scheduled.");
    }
    // Delete the saved confirmation job after the order has been processed.
    public bool Delete(string jobId) => _jobs.Delete(jobId);
}
