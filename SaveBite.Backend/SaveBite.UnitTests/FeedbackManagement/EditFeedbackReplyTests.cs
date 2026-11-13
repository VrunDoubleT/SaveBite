using System.Net;
using FluentAssertions;
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

public class EditFeedbackReplyTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly Mock<INotificationQueue> _notificationsMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly FeedbackService _service;
    private readonly UpdateFeedbackReplyRequestValidator _validator = new();

    public EditFeedbackReplyTests()
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
    public async Task EditFeedbackReplyAsync_WhenReplyAuthorEdits_UpdatesTrimmedContent()
    {
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        var reply = new ProductFeedbackReply
        {
            Id = Guid.NewGuid(),
            ProductFeedbackId = feedbackId,
            RepliedByUserId = userId,
            Content = "Initial reply",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            RepliedByUser = new User { Id = userId, FullName = "Owner Bob" }
        };

        _db.Users.Add(new User
        {
            Id = userId,
            Email = $"owner-{userId:N}@example.com",
            PasswordHash = "hash",
            FullName = "Owner Bob",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(userId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(reply);
        _feedbackRepoMock.Setup(r => r.UpdateReplyAsync(reply, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.EditFeedbackReplyAsync(userId, feedbackId, new FeedbackRequests.UpdateFeedbackReplyRequest
        {
            Content = "  Updated courteous reply  "
        });

        result.Id.Should().Be(reply.Id);
        result.Content.Should().Be("Updated courteous reply");
        result.RepliedByName.Should().Be("Owner Bob");
        result.RepliedByUserId.Should().Be(userId);
        _feedbackRepoMock.Verify(r => r.UpdateReplyAsync(reply, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditFeedbackReplyAsync_WhenAdminEditsAnotherUsersReply_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var originalAuthorId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        var reply = new ProductFeedbackReply
        {
            Id = Guid.NewGuid(),
            ProductFeedbackId = feedbackId,
            RepliedByUserId = originalAuthorId,
            Content = "Old content",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _db.Users.Add(new User
        {
            Id = adminId,
            Email = $"admin-{adminId:N}@example.com",
            PasswordHash = "hash",
            FullName = "Platform Admin",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(adminId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(reply);
        _feedbackRepoMock.Setup(r => r.UpdateReplyAsync(reply, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.EditFeedbackReplyAsync(adminId, feedbackId, new FeedbackRequests.UpdateFeedbackReplyRequest
        {
            Content = "Moderated reply"
        });

        result.Content.Should().Be("Moderated reply");
        result.RepliedByName.Should().Be("Shop");
        _feedbackRepoMock.Verify(r => r.UpdateReplyAsync(reply, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditFeedbackReplyAsync_WhenUserCannotManage_ThrowsForbidden()
    {
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(userId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => _service.EditFeedbackReplyAsync(userId, feedbackId, new FeedbackRequests.UpdateFeedbackReplyRequest { Content = "Edit" });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("You do not have permission to edit replies for this shop's feedback.");
    }

    [Fact]
    public async Task EditFeedbackReplyAsync_WhenReplyNotFound_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();
        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(userId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync((ProductFeedbackReply?)null);

        var act = () => _service.EditFeedbackReplyAsync(userId, feedbackId, new FeedbackRequests.UpdateFeedbackReplyRequest { Content = "Edit" });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Reply not found.");
    }

    [Fact]
    public async Task EditFeedbackReplyAsync_WhenNonAdminEditsAnotherUsersReply_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var feedbackId = Guid.NewGuid();

        _db.Users.Add(new User
        {
            Id = ownerId,
            Email = $"owner-{ownerId:N}@example.com",
            PasswordHash = "hash",
            FullName = "Shop Owner",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _feedbackRepoMock.Setup(r => r.CanManageFeedbackReplyAsync(ownerId, feedbackId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedbackRepoMock.Setup(r => r.GetReplyByFeedbackIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductFeedbackReply
            {
                Id = Guid.NewGuid(),
                ProductFeedbackId = feedbackId,
                RepliedByUserId = staffId,
                Content = "Staff reply"
            });

        var act = () => _service.EditFeedbackReplyAsync(ownerId, feedbackId, new FeedbackRequests.UpdateFeedbackReplyRequest { Content = "Trying edit" });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("You can only edit your own reply.");
        _feedbackRepoMock.Verify(r => r.UpdateReplyAsync(It.IsAny<ProductFeedbackReply>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void UpdateFeedbackReplyRequestValidator_ValidatesEmptyAndMaxLength()
    {
        _validator.Validate(new FeedbackRequests.UpdateFeedbackReplyRequest { Content = "" }).IsValid.Should().BeFalse();
        _validator.Validate(new FeedbackRequests.UpdateFeedbackReplyRequest { Content = "   " }).IsValid.Should().BeFalse();
        _validator.Validate(new FeedbackRequests.UpdateFeedbackReplyRequest { Content = new string('x', 2001) }).IsValid.Should().BeFalse();
        _validator.Validate(new FeedbackRequests.UpdateFeedbackReplyRequest { Content = "Updated text" }).IsValid.Should().BeTrue();
    }
}
