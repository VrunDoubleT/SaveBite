using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.Notifications;

/// <summary>
/// Unit Tests for Feature: View Notifications
/// Service: NotificationService.GetMyNotificationsAsync
/// Rule: BR-017 Only the notification recipient may access or manage their own notifications.
/// Function Code: UT-136
/// Sheet Name: UT_ViewNotifications
/// </summary>
public class ViewNotificationsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<INotificationRepository> _repositoryMock = new();
    private readonly Mock<IRealtimePublisher> _realtimeMock = new();
    private readonly NotificationService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ViewNotificationsTests(ITestOutputHelper output)
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
        string title = "Order Placed",
        string body = "Your order #1234 has been placed successfully.",
        NotificationType type = NotificationType.OrderCreated,
        DateTime? readAt = null,
        DateTime? dismissedAt = null)
    {
        return new Notification
        {
            Id = id,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            ActionUrl = "/orders/1234",
            ReadAt = readAt,
            DismissedAt = dismissedAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // UTCID01: Retrieve all notifications of the authenticated user (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetMyNotifications_AllNotifications_Normal()
    {
        // Arrange
        var request = new NotificationRequests.GetNotificationsRequest
        {
            IsUnreadOnly = false
        };

        var notif1 = CreateNotification(Guid.NewGuid(), _userId, "Order Placed", "Your order has been placed.");
        var notif2 = CreateNotification(Guid.NewGuid(), _userId, "Payment Success", "Payment received.", NotificationType.PaymentSucceeded, readAt: DateTime.UtcNow);
        var items = new List<Notification> { notif1, notif2 };

        _repositoryMock
            .Setup(r => r.GetUserNotificationsPageAsync(_userId, It.IsAny<int>(), It.IsAny<int>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 2, 1));

        // Act
        var result = await _service.GetMyNotificationsAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.UnreadCount.Should().Be(1);
        result.Page.Items.Should().HaveCount(2);

        var first = result.Page.Items[0];
        first.Id.Should().Be(notif1.Id);
        first.Title.Should().Be(notif1.Title);
        first.Body.Should().Be(notif1.Body);
        first.IsRead.Should().BeFalse();

        var second = result.Page.Items[1];
        second.Id.Should().Be(notif2.Id);
        second.IsRead.Should().BeTrue();
        second.ReadAt.Should().NotBeNull();

        _repositoryMock.Verify(r => r.GetUserNotificationsPageAsync(_userId, It.IsAny<int>(), It.IsAny<int>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Retrieve unread notifications only (IsUnreadOnly = true) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetMyNotifications_UnreadOnly_Normal()
    {
        // Arrange
        var request = new NotificationRequests.GetNotificationsRequest
        {
            IsUnreadOnly = true
        };

        var unreadNotif = CreateNotification(Guid.NewGuid(), _userId, "Refund Approved", "Your refund is ready.");
        var items = new List<Notification> { unreadNotif };

        _repositoryMock
            .Setup(r => r.GetUserNotificationsPageAsync(_userId, It.IsAny<int>(), It.IsAny<int>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1, 1));

        // Act
        var result = await _service.GetMyNotificationsAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.UnreadCount.Should().Be(1);
        result.Page.Items.Should().HaveCount(1);
        result.Page.Items[0].IsRead.Should().BeFalse();

        _repositoryMock.Verify(r => r.GetUserNotificationsPageAsync(_userId, It.IsAny<int>(), It.IsAny<int>(), true, It.IsAny<CancellationToken>()), Times.Once);
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
}
