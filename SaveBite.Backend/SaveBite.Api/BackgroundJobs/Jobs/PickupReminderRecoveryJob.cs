using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class PickupReminderRecoveryJob(AppDbContext db, PickupDeadlineReminderJob job, TimeProvider clock)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var candidates = await db.Orders.AsNoTracking().Where(o => o.PickupReminderEmailSentAt == null
            && o.HardDeadlineAt > now && o.HardDeadlineAt <= now.AddMinutes(10)
            && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.ReadyForPickup))
            .Select(o => new { o.Id, o.HardDeadlineAt, o.ConfirmedAt, o.CreatedAt }).ToListAsync(ct);
        foreach (var order in candidates)
            if (PickupReminderPolicy.DueAt(order.ConfirmedAt ?? order.CreatedAt, order.HardDeadlineAt!.Value) <= now)
                await job.ExecuteAsync(order.Id, order.HardDeadlineAt.Value, ct);
    }
}
