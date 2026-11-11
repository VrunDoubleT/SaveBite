using Hangfire;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;
namespace SaveBite.Backend.BackgroundJobs.Scheduling;

public sealed class ShopOrderNoShowScheduler(IBackgroundJobClient jobs, TimeProvider clock,
    ILogger<ShopOrderNoShowScheduler> logger) : IShopOrderNoShowScheduler
{
    public string? Schedule(Guid orderId, DateTime deadline)
    {
        var id = SafeSchedule.Try(() => jobs.Schedule<ShopOrderNoShowJob>(job => job.ExecuteAsync(orderId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(deadline, DateTimeKind.Utc))), logger, "no-show", orderId);
        var reminderAt = PickupReminderPolicy.DueAt(clock.GetUtcNow().UtcDateTime, deadline);
        SafeSchedule.Try(() => jobs.Schedule<PickupDeadlineReminderJob>(job => job.ExecuteAsync(orderId, deadline, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(reminderAt, DateTimeKind.Utc))), logger, "pickup-reminder", orderId);
        return id;
    }
}
