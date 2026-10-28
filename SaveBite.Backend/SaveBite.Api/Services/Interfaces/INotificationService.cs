using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface INotificationService
{
    //  View Notifications
    Task<NotificationListResponse> GetMyNotificationsAsync(
        Guid userId, NotificationRequests.GetNotificationsRequest request, CancellationToken ct = default);

    //  Mark Notification as Read
    Task<NotificationItemResponse> MarkAsReadAsync(
        Guid userId, Guid notificationId, CancellationToken ct = default);

    //  Delete Notification
    Task DeleteNotificationAsync(
        Guid userId, Guid notificationId, CancellationToken ct = default);

    // Shared dispatcher for all 4 roles: persist to DB and push realtime SignalR events to eligible recipients.
    Task SendAsync(
        NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default);
}
