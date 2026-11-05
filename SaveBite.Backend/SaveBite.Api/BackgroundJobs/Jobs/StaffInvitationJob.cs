using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class StaffInvitationJob(IStaffInvitationRepository repository, StaffInvitationNotificationService notifications, SaveBite.Backend.Data.AppDbContext db)
{
    public const int LifetimeDays = 3;

    public async Task ExpireAsync(Guid invitationId, CancellationToken ct)
    {
        await NotificationTransaction.RunAsync(db, async () =>
        {
        await repository.TryTransitionPendingAsync(invitationId, nameof(StaffInvitationStatus.Expired), DateTime.UtcNow, ct);
        // A retry after the transition reuses the same RabbitMQ notification keys.
        await notifications.NotifyAsync(invitationId, StaffInvitationStatus.Expired, ct);
        }, ct);
    }

    // Compatibility for notification jobs already stored by earlier versions.
    // New requests publish directly through StaffInvitationNotificationService.
    public Task NotifyAsync(Guid invitationId, StaffInvitationStatus status, CancellationToken ct)
        => notifications.NotifyAsync(invitationId, status, ct);
}
