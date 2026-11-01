using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.Notifications;

/// <summary>
/// Unit Tests for Feature: Mark Notification as Read
/// Service: NotificationService.MarkAsReadAsync and MarkAllAsReadAsync
/// Rule: BR-017 Only the notification recipient may access or manage their own notifications.
/// </summary>
public class MarkNotificationAsReadTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<INotificationRepository> _repositoryMock = new();
    private readonly Mock<IRealtimePublisher> _realtimeMock = new();
    private readonly NotificationService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _notificationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public MarkNotificationAsReadTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new NotificationService(
            _repositoryMock.Object,
            _realtimeMock.Object,
            TimeProvider.System);
    }

    private Notification CreateNotification(
        Guid id,
        Guid userId,
        DateTime? readAt = null,
        DateTime? dismissedAt = null)
    {
        return new Notification
        {
            Id = id,
            UserId = userId,
            Type = NotificationType.FlashDealAvailable,
            Title = "Discount Flash Deal",
            Body = "Check out our 50% discount flash deal!",
            ActionUrl = "/flash-deals",
            ReadAt = readAt,
            DismissedAt = dismissedAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // UTCID01: Unread Notification -> Marks As Read, Saves, and Emits Realtime Event (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_MarkAsRead_UnreadNotification_MarksReadAndPushesRealtimeSync_Normal()
    {
        // Arrange
        var notification = CreateNotification(_notificationId, _userId, readAt: null);

        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(_notificationId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _repositoryMock
            .Setup(r => r.GetUnreadCountAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _realtimeMock
            .Setup(rt => rt.SendToUserAsync(
                _userId,
                RealtimeEvents.NotificationRead,
                It.IsAny<NotificationStateSyncPayload>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.MarkAsReadAsync(_userId, _notificationId);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Id.Should().Be(_notificationId);
        result.IsRead.Should().BeTrue();
        result.ReadAt.Should().NotBeNull();

        notification.ReadAt.Should().NotBeNull();

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _realtimeMock.Verify(rt => rt.SendToUserAsync(
            _userId,
            RealtimeEvents.NotificationRead,
            It.Is<NotificationStateSyncPayload>(p => p.NotificationId == _notificationId && p.UnreadCount == 0)), Times.Once);
    }

    // =========================================================================
    // UTCID02: Already Read Notification -> Idempotent, Does Not Re-update or Emit Realtime (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_MarkAsRead_AlreadyReadNotification_DoesNotReupdateOrSendRealtime_Normal()
    {
        // Arrange
        var existingReadAt = DateTime.UtcNow.AddMinutes(-10);
        var notification = CreateNotification(_notificationId, _userId, readAt: existingReadAt);

        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(_notificationId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        // Act
        var result = await _service.MarkAsReadAsync(_userId, _notificationId);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Id.Should().Be(_notificationId);
        result.IsRead.Should().BeTrue();
        result.ReadAt.Should().Be(existingReadAt);

        // Verify SaveChanges and Realtime are NOT triggered for idempotency
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _realtimeMock.Verify(rt => rt.SendToUserAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: Notification Belongs to Another User or Non-Existent -> Throws 404 (BR-017 Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_MarkAsRead_NotificationNotFoundOrBelongsToOtherUser_ThrowsNotFound_BR017_Abnormal()
    {
        // Arrange
        var otherUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(_notificationId, otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        // Act
        var act = () => _service.MarkAsReadAsync(otherUserId, _notificationId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.NotificationNotFound);

        _repositoryMock.Verify(r => r.GetByIdForUserAsync(_notificationId, otherUserId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Mark All As Read with Unread Notifications -> Marks and Pushes Realtime (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID04_MarkAllAsRead_WhenUnreadExist_UpdatesRepoAndPushesRealtimeSync_Normal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.MarkAllAsReadAsync(_userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3); // 3 notifications updated

        _realtimeMock
            .Setup(rt => rt.SendToUserAsync(
                _userId,
                RealtimeEvents.NotificationRead,
                It.IsAny<NotificationStateSyncPayload>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.MarkAllAsReadAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID04", new { UpdatedCount = 3 });
        _repositoryMock.Verify(r => r.MarkAllAsReadAsync(_userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _realtimeMock.Verify(rt => rt.SendToUserAsync(
            _userId,
            RealtimeEvents.NotificationRead,
            It.Is<NotificationStateSyncPayload>(p => p.NotificationId == Guid.Empty && p.UnreadCount == 0)), Times.Once);
    }

    // =========================================================================
    // UTCID05: Mark All As Read with No Unread Notifications -> Does Not Emit Realtime (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID05_MarkAllAsRead_WhenNoUnreadExist_DoesNotPushRealtime_Normal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.MarkAllAsReadAsync(_userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _service.MarkAllAsReadAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID05", new { UpdatedCount = 0 });
        _realtimeMock.Verify(rt => rt.SendToUserAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    // =========================================================
    // Helper Methods for Formatting Log Output
    // =========================================================
    private void PrintSuccess<T>(string tcid, T result, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }

    private void PrintException(string tcid, AppException ex)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"ERROR CODE: {ex.ErrorCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }
}
