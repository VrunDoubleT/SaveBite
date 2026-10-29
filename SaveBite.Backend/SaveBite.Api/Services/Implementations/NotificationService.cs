using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
using System.Net;

namespace SaveBite.Backend.Services.Implementations;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IRealtimePublisher _realtime;
    private readonly TimeProvider _clock;

    public NotificationService(
        INotificationRepository notificationRepository,
        IRealtimePublisher realtimePublisher,
        TimeProvider clock)
    {
        _repository = notificationRepository;
        _realtime = realtimePublisher;
        _clock = clock;
    }

    // UC-22: Retrieve paginated notifications for the authenticated user.
    public async Task<NotificationListResponse> GetMyNotificationsAsync(
        Guid userId, NotificationRequests.GetNotificationsRequest request, CancellationToken ct = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var (items, totalItems, unreadCount) = await _repository.GetUserNotificationsPageAsync(
            userId, page, pageSize, request.IsUnreadOnly, ct);

        var dtoItems = items.Select(MapToResponse).ToList();
        var pagedResult = PagedResult<NotificationItemResponse>.Create(dtoItems, page, pageSize, totalItems);

        return new NotificationListResponse(pagedResult, unreadCount);
    }

    // UC-23: Mark a notification as read and sync realtime state across active tabs.
    public async Task<NotificationItemResponse> MarkAsReadAsync(
        Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _repository.GetByIdForUserAsync(notificationId, userId, ct)
            ?? throw new AppException("Notification not found.", ErrorCodes.NotificationNotFound, HttpStatusCode.NotFound);

        if (notification.ReadAt == null)
        {
            notification.ReadAt = _clock.GetUtcNow().UtcDateTime;
            await _repository.SaveChangesAsync(ct);

            var unreadCount = await _repository.GetUnreadCountAsync(userId, ct);
            await _realtime.SendToUserAsync(
                userId,
                RealtimeEvents.NotificationRead,
                new NotificationStateSyncPayload(notification.Id, unreadCount, notification.ReadAt));
        }

        return MapToResponse(notification);
    }

    // UC-24: Soft-delete a notification via DismissedAt and sync realtime state across active tabs.
    public async Task DeleteNotificationAsync(
        Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _repository.GetByIdForUserAsync(notificationId, userId, ct)
            ?? throw new AppException("Notification not found.", ErrorCodes.NotificationNotFound, HttpStatusCode.NotFound);

        notification.DismissedAt = _clock.GetUtcNow().UtcDateTime;
        await _repository.SaveChangesAsync(ct);

        var unreadCount = await _repository.GetUnreadCountAsync(userId, ct);
        await _realtime.SendToUserAsync(
            userId,
            RealtimeEvents.NotificationDeleted,
            new NotificationStateSyncPayload(notification.Id, unreadCount));
    }

    // Shared notification dispatcher: resolve recipients -> persist in DB -> push via SignalR to each recipient.
    public async Task SendAsync(
        NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default)
    {
        var recipientIds = await _repository.ResolveRecipientUserIdsAsync(target, ct);
        if (recipientIds.Count == 0) return;

        var now = _clock.GetUtcNow().UtcDateTime;
        var notifications = recipientIds.Select(userId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = command.Type,
            Title = command.Title.Trim(),
            Body = command.Body.Trim(),
            ActionUrl = string.IsNullOrWhiteSpace(command.ActionUrl) ? null : command.ActionUrl.Trim(),
            DeduplicationKey = string.IsNullOrWhiteSpace(command.DeduplicationKey) ? null : command.DeduplicationKey.Trim(),
            CreatedAt = now
        }).ToList();

        var persistedNotifications = await _repository.CreateManyAsync(notifications, ct);

        // A retry reuses persisted IDs; clients upsert by ID rather than incrementing duplicate entries.
        foreach (var item in persistedNotifications)
        {
            var unreadCount = await _repository.GetUnreadCountAsync(item.UserId, ct);
            var payload = new NotificationRealtimePayload(MapToResponse(item), unreadCount);

            await _realtime.SendToUserAsync(
                item.UserId,
                RealtimeEvents.NotificationReceived,
                payload);
        }
    }

    private static NotificationItemResponse MapToResponse(Notification n) =>
        new(
            n.Id,
            n.Type.ToString(),
            n.Title,
            n.Body,
            n.ActionUrl,
            n.ReadAt.HasValue,
            n.ReadAt,
            n.CreatedAt);
}
