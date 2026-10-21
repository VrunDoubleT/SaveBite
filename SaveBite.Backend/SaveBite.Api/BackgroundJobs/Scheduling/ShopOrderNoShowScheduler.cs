using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.BackgroundJobs.Scheduling;

// Schedule Hangfire no-show processing using the recorded hard deadline.
public sealed class ShopOrderNoShowScheduler : IShopOrderNoShowScheduler
{
    private readonly IBackgroundJobClient _jobs;
    public ShopOrderNoShowScheduler(IBackgroundJobClient jobs) { _jobs = jobs; }

    // Schedule the no-show check at the order's hard pickup deadline.
    public string Schedule(Guid orderId, DateTime deadline)
    {
        var id = _jobs.Schedule<ShopOrderNoShowJob>(job => job.ExecuteAsync(orderId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(deadline, DateTimeKind.Utc)));
        return !string.IsNullOrWhiteSpace(id) ? id : throw new InvalidOperationException("Order NoShow could not be scheduled.");
    }
}
