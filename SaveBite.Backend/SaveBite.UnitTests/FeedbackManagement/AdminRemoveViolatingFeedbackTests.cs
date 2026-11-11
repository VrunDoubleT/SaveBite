using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FeedbackManagement;

/// <summary>
/// Unit Tests for Feature: Remove Violating Feedback
/// Service: FeedbackService.RemoveAsync
/// Function Code: UT-MOD-11
/// </summary>
public class AdminRemoveViolatingFeedbackTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly FeedbackService _service;

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public AdminRemoveViolatingFeedbackTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FeedbackService(
            _feedbackRepoMock.Object,
            new Mock<INotificationQueue>().Object,
            TestDbContextFactory.Create(),
            _auditMock.Object,
            null,
            null
        );
    }

    [Fact]
    public async Task UTCID01_RemoveAsync_ValidFeedback_SetsStatusHiddenByAdminAndPublishesAudit()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var feedback = new ProductFeedback
        {
            Id = feedbackId,
            Status = FeedbackStatus.Visible,
            Comment = "Contains prohibited speech",
            CreatedAt = DateTime.UtcNow
        };

        var request = new HideViolatingFeedbackRequest
        {
            Reason = "Violates community standards on hate speech"
        };

        _feedbackRepoMock.Setup(r => r.GetByIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        // Act
        var result = await _service.RemoveAsync(_adminId, feedbackId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TargetId.Should().Be(feedbackId);
        result.Action.Should().Be("FEEDBACK_HIDDEN");
        result.Message.Should().Be("Feedback removed.");

        feedback.Status.Should().Be(FeedbackStatus.HiddenByAdmin);
        feedback.HiddenAt.Should().NotBeNull();

        _feedbackRepoMock.Verify(r => r.UpdateAsync(feedback, It.IsAny<CancellationToken>()), Times.Once);

        _auditMock.Verify(a => a.Record(It.Is<AuditLog>(log =>
            log.ActorUserId == _adminId &&
            log.Action == AuditActionTypes.FeedbackHidden &&
            log.TargetId == feedbackId)), Times.Once);

        _output.WriteLine("UTCID01 passed: Successfully hid feedback and saved moderation log & published audit event.");
    }

    [Fact]
    public async Task UTCID02_RemoveAsync_FeedbackNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var request = new HideViolatingFeedbackRequest
        {
            Reason = "Violates guidelines"
        };

        _feedbackRepoMock.Setup(r => r.GetByIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductFeedback?)null);

        // Act
        var act = () => _service.RemoveAsync(_adminId, feedbackId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Contain("Feedback not found");
        _output.WriteLine("UTCID02 passed: Non-existent feedback threw 404 NotFound.");
    }

    [Theory]
    [InlineData(FeedbackStatus.HiddenByAdmin)]
    [InlineData(FeedbackStatus.HiddenByCustomer)]
    public async Task UTCID03_RemoveAsync_FeedbackNotVisible_ThrowsConflictException(FeedbackStatus initialStatus)
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var feedback = new ProductFeedback
        {
            Id = feedbackId,
            Status = initialStatus,
            CreatedAt = DateTime.UtcNow
        };

        var request = new HideViolatingFeedbackRequest
        {
            Reason = "Violates content policy"
        };

        _feedbackRepoMock.Setup(r => r.GetByIdAsync(feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        // Act
        var act = () => _service.RemoveAsync(_adminId, feedbackId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Contain("Only visible feedback can be");
        _output.WriteLine($"UTCID03 passed: Feedback with status {initialStatus} threw 409 Conflict.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234")] // 4 characters < 5
    public void UTCID04_RemoveAsync_InvalidReason_ThrowsValidationException(string? invalidReason)
    {
        // Arrange
        var request = new HideViolatingFeedbackRequest
        {
            Reason = invalidReason!
        };
        var validator = new HideViolatingFeedbackRequestValidator();

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
        _output.WriteLine($"UTCID04 passed: Reason '{invalidReason}' failed validation as expected.");
    }
}
