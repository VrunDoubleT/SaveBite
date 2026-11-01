using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Publishes through NotificationQueue immediately; no Hangfire notification dispatch.
public sealed class StaffInvitationNotificationService(IStaffInvitationRepository repository, INotificationQueue notifications)
{
    public const string CustomerUrl = "/account/staff-invitations";
    public const string OwnerUrl = "/store-owner/staff/invitations";

    public async Task NotifyAsync(Guid invitationId, StaffInvitationStatus status, CancellationToken ct)
    {
        var invitation = await repository.GetInvitationByIdAsync(invitationId, ct);
        if (invitation == null || invitation.Status != status.ToString()) return;
        var shop = invitation.Shop.Name;
        var customer = invitation.InvitedUser.FullName;
        async Task Send(Guid userId, NotificationType type, string title, string body, string url, string recipient)
            => await notifications.EnqueueAsync(NotificationTarget.User(userId), new CreateNotificationCommand(
                type, title, body, url, Key(invitationId, status, recipient)), ct);

        switch (status)
        {
            case StaffInvitationStatus.Pending:
                await Send(invitation.InvitedUserId, NotificationType.StaffInvitationReceived,
                    "New staff invitation", $"{shop} invited you to join their team. Respond within 3 days.", CustomerUrl, "customer");
                break;
            case StaffInvitationStatus.Accepted:
                await Send(invitation.Shop.OwnerUserId, NotificationType.StaffInvitationAccepted,
                    "Staff invitation accepted", $"{customer} accepted your invitation and joined {shop}.", OwnerUrl, "owner");
                await Send(invitation.InvitedUserId, NotificationType.StaffJoinedShop,
                    "Welcome to the team", $"You have joined {shop} as a staff member.", CustomerUrl, "customer");
                break;
            case StaffInvitationStatus.Declined:
                await Send(invitation.Shop.OwnerUserId, NotificationType.StaffInvitationDeclined,
                    "Staff invitation declined", $"{customer} declined your invitation to join {shop}.", OwnerUrl, "owner");
                break;
            case StaffInvitationStatus.Expired:
                await Send(invitation.Shop.OwnerUserId, NotificationType.StaffInvitationExpired,
                    "Staff invitation expired", $"The invitation for {customer} to join {shop} expired without a response.", OwnerUrl, "owner");
                await Send(invitation.InvitedUserId, NotificationType.StaffInvitationExpired,
                    "Staff invitation expired", $"Your invitation to join {shop} has expired.", CustomerUrl, "customer");
                break;
        }
    }

    public static string Key(Guid id, StaffInvitationStatus status, string recipient)
        => $"staff-invitation:{id}:{status}:{recipient}";
}
