using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FeedbackManagement;

/// <summary>
/// Unit Tests for Feature: Delete Feedback
/// Service: FeedbackService.DeleteFeedbackAsync
/// Function Code: UT-142
/// Sheet Name: UT_DeleteFeedback
/// Rule: BR-049 A Customer may view and manage only feedback they authored.
/// </summary>
public class DeleteFeedbackTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly FeedbackService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _feedbackId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public DeleteFeedbackTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FeedbackService(_feedbackRepoMock.Object);
    }

    private ProductFeedback CreateFeedback(Guid userId, FeedbackStatus status = FeedbackStatus.Visible)
    {
        return new ProductFeedback
        {
            Id = _feedbackId,
            UserId = userId,
            OrderItemId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Rating = 5,
            Comment = "To be deleted",
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    // =========================================================================
    // UTCID01: Author soft deletes own feedback -> Status changed to HiddenByCustomer (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_DeleteFeedback_ValidAuthor_SoftDeletes_Normal()
    {
        // Arrange
        var feedback = CreateFeedback(_userId, FeedbackStatus.Visible);

        _feedbackRepoMock
            .Setup(r => r.GetByIdAsync(_feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        _feedbackRepoMock
            .Setup(r => r.UpdateAsync(feedback, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteFeedbackAsync(_userId, _feedbackId);

        // Assert & Log
        PrintSuccess("UTCID01", new { FeedbackId = _feedbackId, Deleted = true });
        feedback.Status.Should().Be(FeedbackStatus.HiddenByCustomer);
        feedback.HiddenAt.Should().NotBeNull();

        _feedbackRepoMock.Verify(r => r.UpdateAsync(It.Is<ProductFeedback>(f =>
            f.Id == _feedbackId &&
            f.Status == FeedbackStatus.HiddenByCustomer &&
            f.HiddenAt != null), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: User is not author (BR-049) -> 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID02_DeleteFeedback_NotAuthor_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var otherUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var feedback = CreateFeedback(otherUserId, FeedbackStatus.Visible);

        _feedbackRepoMock
            .Setup(r => r.GetByIdAsync(_feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        // Act
        var act = () => _service.DeleteFeedbackAsync(_userId, _feedbackId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        _feedbackRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ProductFeedback>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: Feedback already hidden -> Idempotent no-op (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID03_DeleteFeedback_AlreadyHidden_DoesNotUpdate_Normal()
    {
        // Arrange
        var feedback = CreateFeedback(_userId, FeedbackStatus.HiddenByCustomer);

        _feedbackRepoMock
            .Setup(r => r.GetByIdAsync(_feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        // Act
        await _service.DeleteFeedbackAsync(_userId, _feedbackId);

        // Assert & Log
        PrintSuccess("UTCID03", new { FeedbackId = _feedbackId, AlreadyHidden = true });
        _feedbackRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ProductFeedback>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Feedback not found -> 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_DeleteFeedback_NotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _feedbackRepoMock
            .Setup(r => r.GetByIdAsync(_feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductFeedback?)null);

        // Act
        var act = () => _service.DeleteFeedbackAsync(_userId, _feedbackId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
