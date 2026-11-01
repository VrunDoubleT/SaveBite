using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;

namespace SaveBite.IntegrationTests.Features.Notifications;

/// <summary>
/// Integration Tests for Notification features:
/// - View Notifications
/// - Mark Notification as Read
/// - Delete Notification
/// Rule: BR-017 Only the notification recipient may access or manage their own notifications.
/// </summary>
public class NotificationIntegrationTests : BaseIntegrationTest
{
    public NotificationIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private Notification CreateNotification(
        Guid userId,
        string title = "Flash Deal Notification",
        string body = "New flash deal available near you!",
        NotificationType type = NotificationType.FlashDealAvailable,
        DateTime? readAt = null,
        DateTime? dismissedAt = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            ActionUrl = "/flash-deals",
            ReadAt = readAt,
            DismissedAt = dismissedAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // 1. View Notifications Tests
    // =========================================================================

    [Fact]
    public async Task GetMyNotifications_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Act
        var response = await Client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyNotifications_ReturnsOnlyNotificationsBelongingToCurrentUser_BR017_200()
    {
        // Arrange
        var customerA = await AuthenticateAsCustomerAsync("customerA@savebite.com", "Customer A");

        // Create other user B
        var userB = new User
        {
            Id = Guid.NewGuid(),
            Email = "customerB@savebite.com",
            Phone = "0988888888",
            FullName = "Customer B",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(userB);

        var notifA1 = CreateNotification(customerA.Id, "Order Placed", "Your order has been placed.");
        var notifA2 = CreateNotification(customerA.Id, "Order Completed", "Your order has been completed.", readAt: DateTime.UtcNow);
        var notifB = CreateNotification(userB.Id, "Secret B", "This notification belongs exclusively to Customer B.");

        DbContext.Notifications.AddRange(notifA1, notifA2, notifB);
        await DbContext.SaveChangesAsync();

        // Act - Logged in as Customer A
        var response = await Client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<NotificationListResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Page.Items.Should().HaveCount(2);
        body.Data.Page.Items.Select(x => x.Id).Should().Contain(new[] { notifA1.Id, notifA2.Id });
        body.Data.Page.Items.Select(x => x.Id).Should().NotContain(notifB.Id); // BR-017: Customer A cannot see Customer B's notifications
        body.Data.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMyNotifications_WhenFilteredByUnreadOnly_ReturnsOnlyUnread_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var unreadNotif = CreateNotification(customer.Id, "Unread Title", "Unread body", readAt: null);
        var readNotif = CreateNotification(customer.Id, "Read Title", "Read body", readAt: DateTime.UtcNow);

        DbContext.Notifications.AddRange(unreadNotif, readNotif);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/notifications?isUnreadOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<NotificationListResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Page.Items.Should().ContainSingle(n => n.Id == unreadNotif.Id);
        body.Data.Page.Items.Should().NotContain(n => n.Id == readNotif.Id);
    }

    [Fact]
    public async Task GetMyNotifications_ExcludesDismissedNotifications_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var activeNotif = CreateNotification(customer.Id, "Active Title", "Active body");
        var dismissedNotif = CreateNotification(customer.Id, "Dismissed Title", "Dismissed body", dismissedAt: DateTime.UtcNow);

        DbContext.Notifications.AddRange(activeNotif, dismissedNotif);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<NotificationListResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Page.Items.Should().ContainSingle(n => n.Id == activeNotif.Id);
        body.Data.Page.Items.Should().NotContain(n => n.Id == dismissedNotif.Id);
    }

    // =========================================================================
    // 2. Mark Notification as Read Tests
    // =========================================================================

    [Fact]
    public async Task MarkAsRead_WhenNotificationBelongsToUser_MarksReadAndUpdatesDb_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var notif = CreateNotification(customer.Id, "Order Placed", "Unread notification", readAt: null);
        DbContext.Notifications.Add(notif);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PatchAsync($"/api/notifications/{notif.Id}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<NotificationItemResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(notif.Id);
        body.Data.IsRead.Should().BeTrue();
        body.Data.ReadAt.Should().NotBeNull();

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var updatedInDb = await DbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == notif.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsRead_WhenNotificationAlreadyRead_IsIdempotent_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();
        var initialReadAt = DateTime.UtcNow.AddHours(-1);

        var notif = CreateNotification(customer.Id, "Already Read", "Already read body", readAt: initialReadAt);
        DbContext.Notifications.Add(notif);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PatchAsync($"/api/notifications/{notif.Id}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<NotificationItemResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.IsRead.Should().BeTrue();

        // Verify Database: timestamp was preserved (not overwritten)
        DbContext.ChangeTracker.Clear();
        var updatedInDb = await DbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == notif.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.ReadAt.Should().BeCloseTo(initialReadAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task MarkAsRead_WhenNotificationBelongsToAnotherUser_ReturnsNotFound_BR017_404()
    {
        // Arrange
        var customerA = await AuthenticateAsCustomerAsync("caller@savebite.com", "Caller User");

        var victimUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "victim@savebite.com",
            Phone = "0977777777",
            FullName = "Victim User",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(victimUser);

        var victimNotif = CreateNotification(victimUser.Id, "Victim Notification", "Private data", readAt: null);
        DbContext.Notifications.Add(victimNotif);
        await DbContext.SaveChangesAsync();

        // Act - Caller tries to mark Victim's notification as read
        var response = await Client.PatchAsync($"/api/notifications/{victimNotif.Id}/read", null);

        // Assert - BR-017: Must return 404 NotificationNotFound
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify Database: Victim's notification is still unread
        DbContext.ChangeTracker.Clear();
        var dbNotif = await DbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == victimNotif.Id);
        dbNotif.Should().NotBeNull();
        dbNotif!.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task MarkAllAsRead_MarksAllUserNotificationsAsRead_200()
    {
        // Arrange
        var customerA = await AuthenticateAsCustomerAsync("userA@savebite.com", "User A");

        var userB = new User
        {
            Id = Guid.NewGuid(),
            Email = "userB@savebite.com",
            Phone = "0966666666",
            FullName = "User B",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(userB);

        var notifA1 = CreateNotification(customerA.Id, "Unread A1", "Body", readAt: null);
        var notifA2 = CreateNotification(customerA.Id, "Unread A2", "Body", readAt: null);
        var notifB = CreateNotification(userB.Id, "Unread B", "Body", readAt: null);

        DbContext.Notifications.AddRange(notifA1, notifA2, notifB);
        await DbContext.SaveChangesAsync();

        // Act - User A marks all as read
        var response = await Client.PatchAsync("/api/notifications/read-all", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: User A's notifications are marked as read, but User B's notification remains unread
        DbContext.ChangeTracker.Clear();
        var userANotifs = await DbContext.Notifications.AsNoTracking().Where(n => n.UserId == customerA.Id).ToListAsync();
        userANotifs.Should().AllSatisfy(n => n.ReadAt.Should().NotBeNull());

        var userBNotifInDb = await DbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == notifB.Id);
        userBNotifInDb.Should().NotBeNull();
        userBNotifInDb!.ReadAt.Should().BeNull(); // BR-017: User B's notification untouched
    }

    // =========================================================================
    // 3. Delete Notification Tests
    // =========================================================================

    [Fact]
    public async Task DeleteNotification_WhenNotificationBelongsToUser_SoftDeletes_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var notif = CreateNotification(customer.Id, "To Delete", "This will be deleted.");
        DbContext.Notifications.Add(notif);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/notifications/{notif.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: Soft delete (DismissedAt != null)
        DbContext.ChangeTracker.Clear();
        var deletedInDb = await DbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == notif.Id);
        deletedInDb.Should().NotBeNull();
        deletedInDb!.DismissedAt.Should().NotBeNull();

        // Verify calling GetMyNotifications no longer returns the deleted notification
        var getResponse = await Client.GetAsync("/api/notifications");
        var body = await getResponse.Content.ReadFromJsonAsync<ApiResponse<NotificationListResponse>>();
        body.Should().NotBeNull();
        body!.Data!.Page.Items.Should().NotContain(n => n.Id == notif.Id);
    }

    [Fact]
    public async Task DeleteNotification_WhenNotificationBelongsToAnotherUser_ReturnsNotFound_BR017_404()
    {
        // Arrange
        var customerA = await AuthenticateAsCustomerAsync("callerA@savebite.com", "Caller A");

        var victimUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "victimA@savebite.com",
            Phone = "0955555555",
            FullName = "Victim A",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(victimUser);

        var victimNotif = CreateNotification(victimUser.Id, "Victim Notification", "Must not be deleted by A.");
        DbContext.Notifications.Add(victimNotif);
        await DbContext.SaveChangesAsync();

        // Act - Customer A attempts to delete victim's notification
        var response = await Client.DeleteAsync($"/api/notifications/{victimNotif.Id}");

        // Assert - BR-017: 404 NotificationNotFound
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify Database: Victim's notification is NOT dismissed
        DbContext.ChangeTracker.Clear();
        var dbNotif = await DbContext.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == victimNotif.Id);
        dbNotif.Should().NotBeNull();
        dbNotif!.DismissedAt.Should().BeNull();
    }
}
