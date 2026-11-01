using SaveBite.Backend.Helpers;
using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.BackgroundJobs.Scheduling;

// Schedule Hangfire no-show processing using the recorded hard deadline.
public sealed class ShopOrderNoShowScheduler : IShopOrderNoShowScheduler
{
    private readonly IBackgroundJobClient _jobs;
    private readonly TimeProvider _clock;
    public ShopOrderNoShowScheduler(IBackgroundJobClient jobs, TimeProvider clock) { _jobs = jobs; _clock = clock; }

    // Schedule the no-show check at the order's hard pickup deadline.
    public string Schedule(Guid orderId, DateTime deadline)
    {
        var id = _jobs.Schedule<ShopOrderNoShowJob>(job => job.ExecuteAsync(orderId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(deadline, DateTimeKind.Utc)));
        var reminderAt = PickupReminderPolicy.DueAt(_clock.GetUtcNow().UtcDateTime, deadline);
        _jobs.Schedule<PickupDeadlineReminderJob>(job => job.ExecuteAsync(orderId, deadline, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(reminderAt, DateTimeKind.Utc)));
        return !string.IsNullOrWhiteSpace(id) ? id : throw new InvalidOperationException("Order NoShow could not be scheduled.");
    }
}
