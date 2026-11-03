using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
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
/// Unit Tests for Feature: Edit Feedback
/// Service: FeedbackService.EditFeedbackAsync
/// Function Code: UT-141
/// Sheet Name: UT_EditFeedback
/// Rule: BR-049 A Customer may view and manage only feedback they authored.
/// </summary>
public class EditFeedbackTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly FeedbackService _service;
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _feedbackId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public EditFeedbackTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FeedbackService(_feedbackRepoMock.Object, _publisherMock.Object);
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
            Comment = "Initial positive comment",
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            Product = new Product { Id = Guid.NewGuid(), Name = "Croissant" },
            OrderItem = new OrderItem { Id = Guid.NewGuid(), ProductNameSnapshot = "Croissant" }
        };
    }

    // =========================================================================
    // UTCID01: Author edits feedback rating, comment, and images -> Successfully updated (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_EditFeedback_ValidAuthor_UpdatesAndReturns_Normal()
    {
        // Arrange
        var request = new FeedbackRequests.UpdateFeedbackRequest
        {
            Rating = 4,
            Comment = "Updated review: still good but took a while.",
            Images = new List<string> { "https://cloudinary.com/new_img.jpg" }
        };

        var feedback = CreateFeedback(_userId);

        _feedbackRepoMock
            .Setup(r => r.GetByIdAsync(_feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        _feedbackRepoMock
            .Setup(r => r.ReplaceFeedbackImagesAsync(_feedbackId, It.IsAny<List<ProductFeedbackImage>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _feedbackRepoMock
            .Setup(r => r.UpdateAsync(feedback, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.EditFeedbackAsync(_userId, _feedbackId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Rating.Should().Be(4);
        result.Comment.Should().Be("Updated review: still good but took a while.");

        _feedbackRepoMock.Verify(r => r.UpdateAsync(It.Is<ProductFeedback>(f =>
            f.Id == _feedbackId && f.Rating == 4 && f.Comment == "Updated review: still good but took a while."), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: User is not the author (BR-049) -> 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID02_EditFeedback_NotAuthor_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var otherUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var request = new FeedbackRequests.UpdateFeedbackRequest
        {
            Rating = 3,
            Comment = "Attempt unauthorized update",
            Images = null
        };
        var feedback = CreateFeedback(otherUserId);

        _feedbackRepoMock
            .Setup(r => r.GetByIdAsync(_feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        // Act
        var act = () => _service.EditFeedbackAsync(_userId, _feedbackId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =========================================================================
    // UTCID03: Feedback is hidden or removed (Status != Visible) -> 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_EditFeedback_StatusNotVisible_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new FeedbackRequests.UpdateFeedbackRequest
        {
            Rating = 3,
            Comment = "Attempt update on removed feedback",
            Images = null
        };
        var feedback = CreateFeedback(_userId, FeedbackStatus.HiddenByCustomer);

        _feedbackRepoMock
            .Setup(r => r.GetByIdAsync(_feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedback);

        // Act
        var act = () => _service.EditFeedbackAsync(_userId, _feedbackId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // UTCID04: Feedback not found -> 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_EditFeedback_NotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new FeedbackRequests.UpdateFeedbackRequest
        {
            Rating = 4,
            Comment = "Good",
            Images = null
        };

        _feedbackRepoMock
            .Setup(r => r.GetByIdAsync(_feedbackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductFeedback?)null);

        // Act
        var act = () => _service.EditFeedbackAsync(_userId, _feedbackId, request);

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
