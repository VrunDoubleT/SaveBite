using SaveBite.Backend.Constants;
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
                    NotificationMessages.Staff.StaffInvitationReceivedTitle,
                    NotificationMessages.Staff.StaffInvitationReceivedBody(shop), CustomerUrl, "customer");
                break;
            case StaffInvitationStatus.Accepted:
                await Send(invitation.Shop.OwnerUserId, NotificationType.StaffInvitationAccepted,
                    NotificationMessages.Staff.StaffInvitationAcceptedTitle,
                    NotificationMessages.Staff.StaffInvitationAcceptedBody(customer, shop), OwnerUrl, "owner");
                await Send(invitation.InvitedUserId, NotificationType.StaffJoinedShop,
                    NotificationMessages.Staff.StaffJoinedShopTitle,
                    NotificationMessages.Staff.StaffJoinedShopBody(shop), CustomerUrl, "customer");
                break;
            case StaffInvitationStatus.Declined:
                await Send(invitation.Shop.OwnerUserId, NotificationType.StaffInvitationDeclined,
                    NotificationMessages.Staff.StaffInvitationDeclinedTitle,
                    NotificationMessages.Staff.StaffInvitationDeclinedBody(customer, shop), OwnerUrl, "owner");
                break;
            case StaffInvitationStatus.Expired:
                await Send(invitation.Shop.OwnerUserId, NotificationType.StaffInvitationExpired,
                    NotificationMessages.Staff.StaffInvitationExpiredTitle,
                    NotificationMessages.Staff.StaffInvitationExpiredOwnerBody(customer, shop), OwnerUrl, "owner");
                await Send(invitation.InvitedUserId, NotificationType.StaffInvitationExpired,
                    NotificationMessages.Staff.StaffInvitationExpiredTitle,
                    NotificationMessages.Staff.StaffInvitationExpiredCustomerBody(shop), CustomerUrl, "customer");
                break;
        }
    }

    public static string Key(Guid id, StaffInvitationStatus status, string recipient)
        => $"staff-invitation:{id}:{status}:{recipient}";
}
