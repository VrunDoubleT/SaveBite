using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;

namespace SaveBite.UnitTests.FeedbackManagement;

public class ReplyFeedbackTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly Mock<INotificationQueue> _notificationsMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly FeedbackService _service;
    private readonly CreateFeedbackReplyRequestValidator _validator = new();

    public ReplyFeedbackTests()
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
    public async Task ReplyFeedbackAsync_WhenStoreOwnerReplies_CreatesReplyLogsActivityAndEnqueuesNotification()
    {
        var ownerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();

        _db.Users.Add(new User
        {
            Id = ownerId,
            Email = $"owner-{ownerId:N}@example.com",
            PasswordHash = "hash",
            FullName = "Shop Owner Alice",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var shop = new Shop { Id = shopId, OwnerUserId = ownerId, Name = "Morning Bread" };
        var feedback = new ProductFeedback
        {
            Id = feedbackId,
            UserId = customerId,
            Status = FeedbackStatus.Visible,
            Product = new Product { Id = Guid.NewGuid(), ShopId = shopId, Shop = shop, Name = "Sourdough" },
            OrderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Order = new Order { Id = orderId, ShopId = shopId, Shop = shop }
            }
        };

        _feedbackRepoMock
            .Setup(r => r.CanManageFeedbackReplyAsync(ownerId, feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _feedbackRepoMock
            .Setup(r => r.GetByIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);
        _feedbackRepoMock
            .Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductFeedbackReply?)null);
        _feedbackRepoMock
            .Setup(r => r.CreateReplyAsync(It.IsAny<ProductFeedbackReply>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductFeedbackReply r, CancellationToken _) =>
            {
                r.RepliedByUser = new User { Id = ownerId, FullName = "Shop Owner Alice" };
                return r;
            });

        var result = await _service.ReplyFeedbackAsync(
            ownerId,
            feedbackId,
            new FeedbackRequests.CreateFeedbackReplyRequest { Content = "  Thanks for visiting us!  " });

        result.Content.Should().Be("Thanks for visiting us!");
        result.RepliedByUserId.Should().Be(ownerId);
        result.RepliedByName.Should().Be("Shop Owner Alice");

        var log = await _db.StaffActivityLogs.SingleAsync();
        log.ShopId.Should().Be(shopId);
        log.ActionBy.Should().Be(ownerId);
        log.OrderId.Should().Be(orderId);
        log.Action.Should().Be(StaffActivityAction.FeedbackReplied);
        log.ActorRoleSnapshot.Should().Be("StoreOwner");
        log.ActorNameSnapshot.Should().Be("Shop Owner Alice");

        _notificationsMock.Verify(n => n.EnqueueAsync(
            It.IsAny<NotificationTarget>(),
            It.Is<CreateNotificationCommand>(c =>
                c.Type == NotificationType.FeedbackReplied),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplyFeedbackAsync_WhenActiveStaffReplies_LogsStaffRoleAndDisplayName()
    {
        var ownerId = Guid.NewGuid();
        var staffUserId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();

        _db.ShopStaffMembers.Add(new ShopStaff
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            UserId = staffUserId,
            DisplayName = "  Cashier Minh  ",
            Status = ShopStaffStatus.Active,
            JoinedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var shop = new Shop { Id = shopId, OwnerUserId = ownerId, Name = "Morning Bread" };
        var feedback = new ProductFeedback
        {
            Id = feedbackId,
            UserId = Guid.NewGuid(),
            Status = FeedbackStatus.Visible,
            Product = new Product { Id = Guid.NewGuid(), ShopId = shopId, Shop = shop, Name = "Muffin" },
            OrderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Order = new Order { Id = orderId, ShopId = shopId, Shop = shop }
            }
        };

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(staffUserId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetByIdAsync(feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(feedback);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync((ProductFeedbackReply?)null);
        _feedbackRepoMock.Setup(r => r.CreateReplyAsync(It.IsAny<ProductFeedbackReply>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductFeedbackReply r, CancellationToken _) => r);

        await _service.ReplyFeedbackAsync(staffUserId, feedbackId, new FeedbackRequests.CreateFeedbackReplyRequest { Content = "Appreciate it!" });

        var log = await _db.StaffActivityLogs.SingleAsync();
        log.ActorRoleSnapshot.Should().Be("Staff");
        log.ActorNameSnapshot.Should().Be("Cashier Minh");
    }

    [Fact]
    public async Task ReplyFeedbackAsync_WhenUserCannotManage_ThrowsForbidden()
    {
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        _feedbackRepoMock
            .Setup(r => r.CanManageFeedbackReplyAsync(userId, feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _service.ReplyFeedbackAsync(userId, feedbackId, new FeedbackRequests.CreateFeedbackReplyRequest { Content = "Hello" });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("You do not have permission to reply to this shop's feedback.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReplyFeedbackAsync_WhenFeedbackMissingOrNotVisible_ThrowsNotFound(bool returnNullFeedback)
    {
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(userId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetByIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnNullFeedback ? null : new ProductFeedback { Id = feedbackId, Status = FeedbackStatus.HiddenByAdmin });

        var act = () => _service.ReplyFeedbackAsync(userId, feedbackId, new FeedbackRequests.CreateFeedbackReplyRequest { Content = "Reply" });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Feedback not found or is no longer visible.");
    }

    [Fact]
    public async Task ReplyFeedbackAsync_WhenReplyAlreadyExists_ThrowsConflict()
    {
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(userId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetByIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductFeedback { Id = feedbackId, Status = FeedbackStatus.Visible });
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductFeedbackReply { Id = Guid.NewGuid(), ProductFeedbackId = feedbackId });

        var act = () => _service.ReplyFeedbackAsync(userId, feedbackId, new FeedbackRequests.CreateFeedbackReplyRequest { Content = "Second reply" });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("A reply already exists for this feedback. You can edit the existing reply instead.");
    }

    [Fact]
    public void CreateFeedbackReplyRequestValidator_ValidatesEmptyAndMaxLength()
    {
        _validator.Validate(new FeedbackRequests.CreateFeedbackReplyRequest { Content = "" }).IsValid.Should().BeFalse();
        _validator.Validate(new FeedbackRequests.CreateFeedbackReplyRequest { Content = "   " }).IsValid.Should().BeFalse();
        _validator.Validate(new FeedbackRequests.CreateFeedbackReplyRequest { Content = new string('a', 2001) }).IsValid.Should().BeFalse();
        _validator.Validate(new FeedbackRequests.CreateFeedbackReplyRequest { Content = new string('a', 2000) }).IsValid.Should().BeTrue();
    }
}
