using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.UnitTests.FeedbackManagement;

public class DeleteFeedbackReplyTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly Mock<INotificationQueue> _notificationsMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly FeedbackService _service;

    public DeleteFeedbackReplyTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new FeedbackService(
            _feedbackRepoMock.Object,
            _notificationsMock.Object,
            _db,
            _auditMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task DeleteFeedbackReplyAsync_WhenAuthorDeletesOwnReply_DeletesWithoutNotification()
    {
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        var reply = new ProductFeedbackReply
        {
            Id = Guid.NewGuid(),
            ProductFeedbackId = feedbackId,
            RepliedByUserId = userId,
            Content = "My reply"
        };

        _db.Users.Add(new User
        {
            Id = userId,
            Email = $"owner-{userId:N}@example.com",
            PasswordHash = "hash",
            FullName = "Owner",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(userId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(reply);
        _feedbackRepoMock.Setup(r => r.IsShopOwnerForFeedbackAsync(userId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.DeleteReplyAsync(reply, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.DeleteFeedbackReplyAsync(userId, feedbackId);

        _feedbackRepoMock.Verify(r => r.DeleteReplyAsync(reply, It.IsAny<CancellationToken>()), Times.Once);
        _notificationsMock.Verify(n => n.EnqueueAsync(It.IsAny<NotificationTarget>(), It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFeedbackReplyAsync_WhenShopOwnerDeletesStaffReply_DeletesAndNotifiesStaff()
    {
        var ownerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        var reply = new ProductFeedbackReply
        {
            Id = Guid.NewGuid(),
            ProductFeedbackId = feedbackId,
            RepliedByUserId = staffId,
            Content = "Staff reply"
        };

        _db.Users.Add(new User
        {
            Id = ownerId,
            Email = $"owner-{ownerId:N}@example.com",
            PasswordHash = "hash",
            FullName = "Owner Linh",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(ownerId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(reply);
        _feedbackRepoMock.Setup(r => r.IsShopOwnerForFeedbackAsync(ownerId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.DeleteReplyAsync(reply, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _feedbackRepoMock.Setup(r => r.GetByIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductFeedback
            {
                Id = feedbackId,
                Product = new Product { Id = Guid.NewGuid(), ShopId = shopId }
            });

        await _service.DeleteFeedbackReplyAsync(ownerId, feedbackId);

        _feedbackRepoMock.Verify(r => r.DeleteReplyAsync(reply, It.IsAny<CancellationToken>()), Times.Once);
        _notificationsMock.Verify(n => n.EnqueueAsync(
            It.Is<NotificationTarget>(t => t.UserIds != null && t.UserIds.Contains(staffId)),
            It.Is<CreateNotificationCommand>(c => c.ActionUrl == $"/staff/shops/{shopId}/reviews"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteFeedbackReplyAsync_WhenAdminDeletesAnyReply_SucceedsWithoutOwnerCheck()
    {
        var adminId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        var reply = new ProductFeedbackReply
        {
            Id = Guid.NewGuid(),
            ProductFeedbackId = feedbackId,
            RepliedByUserId = staffId,
            Content = "Staff reply"
        };

        _db.Users.Add(new User
        {
            Id = adminId,
            Email = $"admin-{adminId:N}@example.com",
            PasswordHash = "hash",
            FullName = "Admin",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(adminId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(reply);
        _feedbackRepoMock.Setup(r => r.DeleteReplyAsync(reply, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.DeleteFeedbackReplyAsync(adminId, feedbackId);

        _feedbackRepoMock.Verify(r => r.DeleteReplyAsync(reply, It.IsAny<CancellationToken>()), Times.Once);
        _feedbackRepoMock.Verify(r => r.IsShopOwnerForFeedbackAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFeedbackReplyAsync_WhenUserCannotManage_ThrowsForbidden()
    {
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(userId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => _service.DeleteFeedbackReplyAsync(userId, feedbackId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("You do not have permission to delete replies for this shop's feedback.");
    }

    [Fact]
    public async Task DeleteFeedbackReplyAsync_WhenReplyNotFound_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(userId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync((ProductFeedbackReply?)null);

        var act = () => _service.DeleteFeedbackReplyAsync(userId, feedbackId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Reply not found.");
    }

    [Fact]
    public async Task DeleteFeedbackReplyAsync_WhenNonOwnerStaffDeletesAnotherUsersReply_ThrowsForbidden()
    {
        var staffA = Guid.NewGuid();
        var staffB = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();

        _db.Users.Add(new User
        {
            Id = staffA,
            Email = $"staff-{staffA:N}@example.com",
            PasswordHash = "hash",
            FullName = "Staff A",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(staffA, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductFeedbackReply { Id = Guid.NewGuid(), ProductFeedbackId = feedbackId, RepliedByUserId = staffB });
        _feedbackRepoMock.Setup(r => r.IsShopOwnerForFeedbackAsync(staffA, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => _service.DeleteFeedbackReplyAsync(staffA, feedbackId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("You can only delete your own reply.");
        _feedbackRepoMock.Verify(r => r.DeleteReplyAsync(It.IsAny<ProductFeedbackReply>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
