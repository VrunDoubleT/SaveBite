using SaveBite.Backend.Services.Interfaces;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FeedbackManagement;

/// <summary>
/// Unit Tests for Feature: View Feedback History
/// Service: FeedbackService.GetMyFeedbackHistoryAsync
/// Function Code: UT-140
/// Sheet Name: UT_ViewFeedbackHistory
/// Rule: BR-049 A Customer may view and manage only feedback they authored.
/// </summary>
public class ViewFeedbackHistoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly FeedbackService _service;
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ViewFeedbackHistoryTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FeedbackService(_feedbackRepoMock.Object, new Mock<INotificationQueue>().Object, TestDbContextFactory.Create(), new Mock<IAdminAuditWriter>().Object, null, null);
    }

    private ProductFeedback CreateFeedback(Guid id, Guid userId, int rating, string comment)
    {
        return new ProductFeedback
        {
            Id = id,
            UserId = userId,
            OrderItemId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Rating = rating,
            Comment = comment,
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Product = new Product { Id = Guid.NewGuid(), Name = "Croissant" },
            OrderItem = new OrderItem { Id = Guid.NewGuid(), ProductNameSnapshot = "Croissant" }
        };
    }

    // =========================================================================
    // UTCID01: Retrieve all feedbacks authored by customer (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetMyFeedbackHistory_CustomerFeedbacks_ReturnsHistory_Normal()
    {
        // Arrange
        var fb1 = CreateFeedback(Guid.NewGuid(), _userId, 5, "Great food!");
        var fb2 = CreateFeedback(Guid.NewGuid(), _userId, 4, "Good service");
        var items = new List<ProductFeedback> { fb1, fb2 };
        var paged = PagedResult<ProductFeedback>.Create(items, 1, 10, 2);

        _feedbackRepoMock
            .Setup(r => r.GetCustomerFeedbackHistoryAsync(_userId, It.IsAny<int>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        // Act
        var result = await _service.GetMyFeedbackHistoryAsync(_userId, 1, 10);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);

        _feedbackRepoMock.Verify(r => r.GetCustomerFeedbackHistoryAsync(
            _userId, It.IsAny<int>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Retrieve specific feedback via targetFeedbackId (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetMyFeedbackHistory_TargetFeedbackId_ReturnsTargetItem_Normal()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var targetFb = CreateFeedback(targetId, _userId, 5, "Specific target feedback");
        var paged = PagedResult<ProductFeedback>.Create(new List<ProductFeedback> { targetFb }, 1, 10, 1);

        _feedbackRepoMock
            .Setup(r => r.GetCustomerFeedbackHistoryAsync(_userId, It.IsAny<int>(), It.IsAny<int>(), targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        // Act
        var result = await _service.GetMyFeedbackHistoryAsync(_userId, 1, 10, targetFeedbackId: targetId);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Items.Should().ContainSingle(f => f.Id == targetId);

        _feedbackRepoMock.Verify(r => r.GetCustomerFeedbackHistoryAsync(
            _userId, It.IsAny<int>(), It.IsAny<int>(), targetId, It.IsAny<CancellationToken>()), Times.Once);
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
