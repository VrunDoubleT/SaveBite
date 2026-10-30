using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/notifications")]
[AccountAccess]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // View Notifications
    [HttpGet]
    public async Task<ActionResult<ApiResponse<NotificationListResponse>>> GetMyNotifications(
        [FromQuery] NotificationRequests.GetNotificationsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetMyNotificationsAsync(
            GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<NotificationListResponse>.Ok(result, "Get notifications successfully."));
    }

    // Mark All Notifications as Read
    [HttpPatch("read-all")]
    public async Task<ActionResult<ApiResponse>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(GetAuthenticatedUserId(), cancellationToken);
        return Ok(ApiResponse.Ok("All notifications marked as read."));
    }

    // Mark Notification as Read
    [HttpPatch("{notificationId:guid}/read")]
    public async Task<ActionResult<ApiResponse<NotificationItemResponse>>> MarkAsRead(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAsReadAsync(
            GetAuthenticatedUserId(), notificationId, cancellationToken);
        return Ok(ApiResponse<NotificationItemResponse>.Ok(result, "Notification marked as read."));
    }

    // Delete Notification
    [HttpDelete("{notificationId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteNotification(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        await _notificationService.DeleteNotificationAsync(
            GetAuthenticatedUserId(), notificationId, cancellationToken);
        return Ok(ApiResponse.Ok("Notification deleted successfully."));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out var userId) ? userId : throw AppException.Unauthorized();
    }
}
