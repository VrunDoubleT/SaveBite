using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Repairs scheduling/publish gaps after a restart or an unavailable broker without adding duplicate notifications.
public sealed class StaffInvitationRecoveryJob(
    AppDbContext db, StaffInvitationJob job, IStaffInvitationScheduler scheduler, StaffInvitationNotificationService notifications)
{
    [DisableConcurrentExecution(300)]
    public async Task ExecuteAsync(bool restoreSchedules, CancellationToken ct)
    {
        var offset = 0;
        while (true)
        {
            var batch = await db.StaffInvitations.AsNoTracking()
                .Where(i => i.Status != nameof(StaffInvitationStatus.Cancelled))
                .OrderBy(i => i.InvitedAt).ThenBy(i => i.Id)
                .Skip(offset).Take(100)
                .Select(i => new { i.Id, i.Status, i.InvitedAt }).ToListAsync(ct);
            if (batch.Count == 0) return;
            var expected = new List<(Guid Id, StaffInvitationStatus Status, string[] Keys)>();
            foreach (var invitation in batch)
            {
                if (!Enum.TryParse<StaffInvitationStatus>(invitation.Status, out var status)) continue;
                if (status == StaffInvitationStatus.Pending)
                {
                    var deadline = invitation.InvitedAt.AddDays(StaffInvitationJob.LifetimeDays);
                    if (deadline <= DateTime.UtcNow)
                    {
                        await job.ExpireAsync(invitation.Id, ct);
                        continue;
                    }
                    if (restoreSchedules) scheduler.ScheduleExpiration(invitation.Id, deadline);
                }
                var recipients = status switch
                {
                    StaffInvitationStatus.Pending => new[] { "customer" },
                    StaffInvitationStatus.Declined => new[] { "owner" },
                    StaffInvitationStatus.Accepted or StaffInvitationStatus.Expired => new[] { "owner", "customer" },
                    _ => Array.Empty<string>()
                };
                expected.Add((invitation.Id, status, recipients.Select(r => StaffInvitationNotificationService.Key(invitation.Id, status, r)).ToArray()));
            }
            var keys = expected.SelectMany(e => e.Keys).ToArray();
            var delivered = (await db.Notifications.AsNoTracking()
                .Where(n => n.DeduplicationKey != null && keys.Contains(n.DeduplicationKey))
                .Select(n => n.DeduplicationKey!).ToListAsync(ct)).ToHashSet();
            foreach (var item in expected.Where(e => e.Keys.Any(key => !delivered.Contains(key))))
                await notifications.NotifyAsync(item.Id, item.Status, ct);
            offset += batch.Count;
        }
    }
}
