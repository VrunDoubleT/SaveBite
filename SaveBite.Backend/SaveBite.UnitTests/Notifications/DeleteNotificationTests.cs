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
/// Unit Tests for Feature: Delete Notification
/// Service: NotificationService.DeleteNotificationAsync
/// Rule: BR-017 Only the notification recipient may access or manage their own notifications.
/// </summary>
public class DeleteNotificationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<INotificationRepository> _repositoryMock = new();
    private readonly Mock<IRealtimePublisher> _realtimeMock = new();
    private readonly NotificationService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _notificationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public DeleteNotificationTests(ITestOutputHelper output)
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
            Type = NotificationType.TrustScoreChanged,
            Title = "System Maintenance",
            Body = "Platform maintenance scheduled tonight at 11:00 PM.",
            ActionUrl = null,
            ReadAt = readAt,
            DismissedAt = dismissedAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // UTCID01: Own Notification -> Soft-Deletes via DismissedAt and Emits Realtime Event (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_DeleteNotification_ValidNotification_SoftDeletesAndPushesRealtimeSync_Normal()
    {
        // Arrange
        var notification = CreateNotification(_notificationId, _userId);

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
                RealtimeEvents.NotificationDeleted,
                It.IsAny<NotificationStateSyncPayload>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteNotificationAsync(_userId, _notificationId);

        // Assert & Log
        PrintSuccess("UTCID01", new { NotificationId = _notificationId, Deleted = true });
        notification.DismissedAt.Should().NotBeNull();

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _realtimeMock.Verify(rt => rt.SendToUserAsync(
            _userId,
            RealtimeEvents.NotificationDeleted,
            It.Is<NotificationStateSyncPayload>(p => p.NotificationId == _notificationId && p.UnreadCount == 0)), Times.Once);
    }

    // =========================================================================
    // UTCID02: Notification Belongs to Another User or Already Dismissed -> Throws 404 (BR-017 Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_DeleteNotification_NotFoundOrBelongsToOtherUser_ThrowsNotFound_BR017_Abnormal()
    {
        // Arrange
        var otherUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(_notificationId, otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        // Act
        var act = () => _service.DeleteNotificationAsync(otherUserId, _notificationId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.NotificationNotFound);

        _repositoryMock.Verify(r => r.GetByIdForUserAsync(_notificationId, otherUserId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _realtimeMock.Verify(rt => rt.SendToUserAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: BR-017 Compliance -> Verifies Repository Lookup Uses Authenticated UserId (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_DeleteNotification_VerifiesRecipientIdPassedToRepository_BR017_Normal()
    {
        // Arrange
        var currentUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var notification = CreateNotification(_notificationId, currentUserId);

        _repositoryMock
            .Setup(r => r.GetByIdForUserAsync(_notificationId, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _repositoryMock
            .Setup(r => r.GetUnreadCountAsync(currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteNotificationAsync(currentUserId, _notificationId);

        // Assert & Log
        PrintSuccess("UTCID03", new { NotificationId = _notificationId, VerifiedUser = currentUserId });
        _repositoryMock.Verify(
            r => r.GetByIdForUserAsync(_notificationId, currentUserId, It.IsAny<CancellationToken>()),
            Times.Once,
            "Repository lookup must enforce authenticated user ID (BR-017).");
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
