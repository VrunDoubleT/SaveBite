using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Implementations;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class StaffInvitationRecoveryJob(IServiceScopeFactory scopes, ILogger<StaffInvitationRecoveryJob> logger)
{
    [DisableConcurrentExecution(600)]
    public Task ExecuteAsync(bool restoreSchedules, CancellationToken ct)
        => RecoveryBatch.RunAsync(scopes, logger, "staff-invitation", db => db.StaffInvitations.AsNoTracking()
            .Where(i => i.Status != nameof(StaffInvitationStatus.Cancelled)).Select(i => i.Id), async (sp, id) =>
            {
                var db = sp.GetRequiredService<AppDbContext>();
                var invitation = await db.StaffInvitations.AsNoTracking().SingleAsync(i => i.Id == id, ct);
                if (!Enum.TryParse<StaffInvitationStatus>(invitation.Status, out var status)) return;
                if (status == StaffInvitationStatus.Pending)
                {
                    var deadline = invitation.InvitedAt.AddDays(StaffInvitationJob.LifetimeDays);
                    if (deadline <= DateTime.UtcNow)
                    {
                        await sp.GetRequiredService<StaffInvitationJob>().ExpireAsync(id, ct);
                        return;
                    }
                    if (restoreSchedules) sp.GetRequiredService<IStaffInvitationScheduler>().ScheduleExpiration(id, deadline);
                }
                var recipients = status switch
                {
                    StaffInvitationStatus.Pending => new[] { "customer" },
                    StaffInvitationStatus.Declined => new[] { "owner" },
                    StaffInvitationStatus.Accepted or StaffInvitationStatus.Expired => new[] { "owner", "customer" },
                    _ => Array.Empty<string>()
                };
                var keys = recipients.Select(r => StaffInvitationNotificationService.Key(id, status, r)).ToArray();
                var delivered = await db.Notifications.AsNoTracking().CountAsync(n => n.DeduplicationKey != null && keys.Contains(n.DeduplicationKey), ct);
                if (delivered < keys.Length) await sp.GetRequiredService<StaffInvitationNotificationService>().NotifyAsync(id, status, ct);
            }, ct);
}
