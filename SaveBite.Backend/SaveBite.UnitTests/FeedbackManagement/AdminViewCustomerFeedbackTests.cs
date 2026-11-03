using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FeedbackManagement;

/// <summary>
/// Unit Tests for Feature: View Customer Feedback (Admin)
/// Service: FeedbackService.ListAsync
/// Function Code: UT-MOD-10
/// </summary>
public class AdminViewCustomerFeedbackTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly FeedbackService _service;

    public AdminViewCustomerFeedbackTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FeedbackService(
            _feedbackRepoMock.Object,
            _publisherMock.Object
        );
    }

    [Fact]
    public async Task UTCID01_ListAsync_ValidPagination_ReturnsPagedFeedbacks()
    {
        // Arrange
        int page = 1;
        int pageSize = 10;
        var shop = new Shop { Name = "Green Bakery" };
        var product = new Product { Name = "Croissant", Shop = shop };
        var user = new User { FullName = "John Doe", Phone = "0901234567", Email = "john@example.com" };

        var feedback = new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ProductId = product.Id,
            Product = product,
            Rating = 5,
            Comment = "Excellent pastry!",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow
        };

        var items = new List<ProductFeedback> { feedback };
        _feedbackRepoMock.Setup(r => r.ListAsync(page, pageSize, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));
        _feedbackRepoMock.Setup(r => r.GetCurrentReasonsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string?>());

        // Act
        var result = await _service.ListAsync(page, pageSize, null, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductName.Should().Be("Croissant");
        result.Items[0].CustomerName.Should().Be("John Doe");
        _output.WriteLine("UTCID01 passed: Successfully retrieved paged feedback for admin moderation.");
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task UTCID02_ListAsync_InvalidPagination_ThrowsBadRequestException(int page, int pageSize)
    {
        // Act
        var act = () => _service.ListAsync(page, pageSize, null, null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Invalid pagination");
        _output.WriteLine($"UTCID02 passed: Invalid pagination (page={page}, size={pageSize}) threw 400 BadRequest.");
    }

    [Fact]
    public async Task UTCID03_ListAsync_WithStatusFilterAndSearch_PassesParametersToRepo()
    {
        // Arrange
        int page = 1;
        int pageSize = 10;
        var status = FeedbackStatus.HiddenByAdmin;
        var search = "Croissant";

        _feedbackRepoMock.Setup(r => r.ListAsync(page, pageSize, status, search, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ProductFeedback>(), 0));
        _feedbackRepoMock.Setup(r => r.GetCurrentReasonsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string?>());

        // Act
        var result = await _service.ListAsync(page, pageSize, status, search, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        _feedbackRepoMock.Verify(r => r.ListAsync(page, pageSize, status, search, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("UTCID03 passed: Filter and search parameters accurately passed to repository.");
    }
}
