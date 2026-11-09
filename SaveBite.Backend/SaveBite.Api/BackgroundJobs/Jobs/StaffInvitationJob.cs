using SaveBite.Backend.Configurations;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class StaffInvitationJob(IStaffInvitationRepository repository, StaffInvitationNotificationService notifications, SaveBite.Backend.Data.AppDbContext db, SaveBite.Backend.Services.Interfaces.IRealtimePublisher realtime)
{
    public const int LifetimeDays = TimingConfiguration.StaffInvitations.LifetimeDays;

    public async Task ExpireAsync(Guid invitationId, CancellationToken ct)
    {
        await NotificationTransaction.RunAsync(db, async () =>
        {
        await repository.TryTransitionPendingAsync(invitationId, nameof(StaffInvitationStatus.Expired), DateTime.UtcNow, ct);
        // A retry after the transition reuses the same RabbitMQ notification keys.
        await notifications.NotifyAsync(invitationId, StaffInvitationStatus.Expired, ct);
        }, ct);
        var invitation = await repository.GetInvitationByIdAsync(invitationId, ct);
        if (invitation?.Status == nameof(StaffInvitationStatus.Expired))
            await StaffRealtimeEvents.InvitationChangedAsync(realtime, invitation);
    }

    // Compatibility for notification jobs already stored by earlier versions.
    // New requests publish directly through StaffInvitationNotificationService.
    public Task NotifyAsync(Guid invitationId, StaffInvitationStatus status, CancellationToken ct)
        => notifications.NotifyAsync(invitationId, status, ct);
}
