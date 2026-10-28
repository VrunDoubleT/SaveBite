using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<IReadOnlyList<Guid>> ResolveRecipientUserIdsAsync(
        NotificationTarget target, CancellationToken ct = default);

    Task<IReadOnlyList<Notification>> CreateManyAsync(
        IReadOnlyList<Notification> notifications, CancellationToken ct = default);

    Task<(IReadOnlyList<Notification> Items, int TotalItems, int UnreadCount)> GetUserNotificationsPageAsync(
        Guid userId, int page, int pageSize, bool? isUnreadOnly, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    Task<Notification?> GetByIdForUserAsync(
        Guid notificationId, Guid userId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}