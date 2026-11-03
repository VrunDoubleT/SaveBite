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
/// Unit Tests for Feature: Add Feedback
/// Service: FeedbackService.AddFeedbackAsync
/// Function Code: UT-139
/// Sheet Name: UT_AddFeedback
/// Rules: BR-032, BR-033, BR-034, BR-049
/// </summary>
public class AddFeedbackTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly FeedbackService _service;
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _orderItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _productId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public AddFeedbackTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FeedbackService(_feedbackRepoMock.Object, _publisherMock.Object);
    }

    private OrderItem CreateOrderItem(Guid customerId, OrderStatus orderStatus)
    {
        return new OrderItem
        {
            Id = _orderItemId,
            OrderId = Guid.NewGuid(),
            Order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Status = orderStatus,
                CreatedAt = DateTime.UtcNow
            },
            FlashDealVariant = new FlashDealVariant
            {
                Id = Guid.NewGuid(),
                Variant = new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = _productId,
                    Product = new Product
                    {
                        Id = _productId,
                        Name = "Croissant"
                    }
                }
            }
        };
    }

    // =========================================================================
    // UTCID01: Valid feedback with rating 5, comment, and images -> Created successfully (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_AddFeedback_ValidCompletedOrder_ReturnsCreatedFeedback_Normal()
    {
        // Arrange
        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = _orderItemId,
            Rating = 5,
            Comment = "Delicious and fresh croissant!",
            Images = new List<string> { "https://cloudinary.com/img1.jpg" }
        };

        var orderItem = CreateOrderItem(_userId, OrderStatus.Completed);

        _feedbackRepoMock
            .Setup(r => r.GetOrderItemWithOrderAsync(_orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);

        _feedbackRepoMock
            .Setup(r => r.HasFeedbackForOrderItemAsync(_orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _feedbackRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<ProductFeedback>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductFeedback fb, CancellationToken _) => fb);

        // Act
        var result = await _service.AddFeedbackAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Rating.Should().Be(5);
        result.Comment.Should().Be("Delicious and fresh croissant!");
        result.OrderItemId.Should().Be(_orderItemId);
        result.ProductId.Should().Be(_productId);

        _feedbackRepoMock.Verify(r => r.CreateAsync(It.Is<ProductFeedback>(f =>
            f.UserId == _userId &&
            f.OrderItemId == _orderItemId &&
            f.Rating == 5 &&
            f.Status == FeedbackStatus.Visible), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Valid minimal feedback with rating 4 and null comment (BR-033) -> Normal (N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_AddFeedback_RatingWithoutComment_Succeeds_Normal()
    {
        // Arrange
        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = _orderItemId,
            Rating = 4,
            Comment = null,
            Images = null
        };
        var orderItem = CreateOrderItem(_userId, OrderStatus.Completed);

        _feedbackRepoMock
            .Setup(r => r.GetOrderItemWithOrderAsync(_orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);

        _feedbackRepoMock
            .Setup(r => r.HasFeedbackForOrderItemAsync(_orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _feedbackRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<ProductFeedback>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductFeedback fb, CancellationToken _) => fb);

        // Act
        var result = await _service.AddFeedbackAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Rating.Should().Be(4);
        result.Comment.Should().BeNull();
    }

    // =========================================================================
    // UTCID03: Order item belongs to another customer (BR-032 / BR-049) -> 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_AddFeedback_OrderItemNotOwnedByUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var otherCustomerId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = _orderItemId,
            Rating = 5,
            Comment = "Good",
            Images = null
        };
        var orderItem = CreateOrderItem(otherCustomerId, OrderStatus.Completed);

        _feedbackRepoMock
            .Setup(r => r.GetOrderItemWithOrderAsync(_orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);

        // Act
        var act = () => _service.AddFeedbackAsync(_userId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =========================================================================
    // UTCID04: Order status is not Completed (BR-032) -> 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_AddFeedback_OrderNotCompleted_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = _orderItemId,
            Rating = 5,
            Comment = "Good",
            Images = null
        };
        var orderItem = CreateOrderItem(_userId, OrderStatus.Confirmed);

        _feedbackRepoMock
            .Setup(r => r.GetOrderItemWithOrderAsync(_orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);

        // Act
        var act = () => _service.AddFeedbackAsync(_userId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // UTCID05: Feedback already submitted for this OrderItem (BR-034) -> 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_AddFeedback_AlreadySubmitted_ThrowsConflict_Abnormal()
    {
        // Arrange
        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = _orderItemId,
            Rating = 5,
            Comment = "Good",
            Images = null
        };
        var orderItem = CreateOrderItem(_userId, OrderStatus.Completed);

        _feedbackRepoMock
            .Setup(r => r.GetOrderItemWithOrderAsync(_orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItem);

        _feedbackRepoMock
            .Setup(r => r.HasFeedbackForOrderItemAsync(_orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.AddFeedbackAsync(_userId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // =========================================================================
    // UTCID06: OrderItem not found -> 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID06_AddFeedback_OrderItemNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new FeedbackRequests.CreateFeedbackRequest
        {
            OrderItemId = _orderItemId,
            Rating = 5,
            Comment = "Good",
            Images = null
        };

        _feedbackRepoMock
            .Setup(r => r.GetOrderItemWithOrderAsync(_orderItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderItem?)null);

        // Act
        var act = () => _service.AddFeedbackAsync(_userId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID06", ex.Which);
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
