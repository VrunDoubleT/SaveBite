using SaveBite.Backend.Models.Common;

namespace SaveBite.Backend.Models.Responses;

public sealed record NotificationItemResponse(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string? ActionUrl,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

public sealed record NotificationListResponse(
    PagedResult<NotificationItemResponse> Page,
    int UnreadCount);

public sealed record NotificationRealtimePayload(
    NotificationItemResponse Notification,
    int UnreadCount);

public sealed record NotificationStateSyncPayload(
    Guid NotificationId,
    int UnreadCount,
    DateTime? ReadAt = null);
