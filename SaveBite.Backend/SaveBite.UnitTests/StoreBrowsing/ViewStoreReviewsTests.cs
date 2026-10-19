using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreBrowsing;

/// <summary>
/// Unit Tests for Feature: View Store Reviews
/// Service: ShopService.GetStoreReviewsAsync & StoreReviewsQueryRequestValidator
/// Function Code: UT-31
/// Sheet Name: UT_ViewStoreReviews
/// </summary>
public class ViewStoreReviewsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly ShopService _service;
    private readonly StoreReviewsQueryRequestValidator _validator = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();

    // Fixed Test Identifiers
    private readonly Guid _existingShopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _nonExistentShopId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private readonly Guid _userId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _userId2 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _productId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public ViewStoreReviewsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopService(_shopRepoMock.Object, _userAddressRepoMock.Object);
    }

    private List<ProductFeedback> CreateSampleFeedbacks()
    {
        var product = new Product
        {
            Id = _productId,
            ShopId = _existingShopId,
            Name = "Banh Mi Cha Lua",
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://res.cloudinary.com/savebite/banhmi.png" }
            }
        };

        var feedback1 = new ProductFeedback
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ProductId = _productId,
            Product = product,
            UserId = _userId1,
            User = new User
            {
                Id = _userId1,
                FullName = "Tran Van A",
                AvatarUrl = "https://res.cloudinary.com/savebite/avatar1.png"
            },
            Rating = 5,
            Comment = "Banh mi rat ngon, gion tan!",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Reply = new ProductFeedbackReply
            {
                Id = Guid.NewGuid(),
                RepliedByUserId = Guid.NewGuid(),
                RepliedByUser = new User { FullName = "SaveBite Bakery Owner" },
                Content = "Cam on ban da ung ho quan!",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        var feedback2 = new ProductFeedback
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ProductId = _productId,
            Product = product,
            UserId = _userId2,
            User = new User
            {
                Id = _userId2,
                FullName = "Le Thi B",
                AvatarUrl = null
            },
            Rating = 4,
            Comment = "Giao hang nhanh, dong goi can than.",
            Status = FeedbackStatus.Visible,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            Reply = null
        };

        return new List<ProductFeedback> { feedback1, feedback2 };
    }

    // =========================================================================
    // UTCID01: Xem toàn bộ đánh giá của quán, có phản hồi từ quán (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetStoreReviews_ValidQuery_HasReviewsWithReplies_ShouldReturnSummaryWithReviews_Normal()
    {
        // Arrange
        var request = new StoreReviewsQueryRequest
        {
            Page = 1,
            PageSize = 10,
            Rating = null
        };

        var feedbacks = CreateSampleFeedbacks();
        var breakdown = new Dictionary<int, int> { { 5, 1 }, { 4, 1 } };

        _shopRepoMock
            .Setup(r => r.ShopExistsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _shopRepoMock
            .Setup(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((4.5, 2, breakdown));

        _shopRepoMock
            .Setup(r => r.GetPagedStoreReviewsAsync(_existingShopId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((feedbacks, 2));

        // Act
        var result = await _service.GetStoreReviewsAsync(_existingShopId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.AverageRating.Should().Be(4.5);
        result.TotalReviews.Should().Be(2);
        result.RatingBreakdown.Should().ContainKey(5).WhoseValue.Should().Be(1);
        result.RatingBreakdown.Should().ContainKey(4).WhoseValue.Should().Be(1);

        result.Reviews.Should().NotBeNull();
        result.Reviews.TotalItems.Should().Be(2);
        result.Reviews.Items.Should().HaveCount(2);

        var firstReview = result.Reviews.Items.First();
        firstReview.Rating.Should().Be(5);
        firstReview.UserFullName.Should().Be("Tran Van A");
        firstReview.ProductName.Should().Be("Banh Mi Cha Lua");
        firstReview.ProductImageUrl.Should().Be("https://res.cloudinary.com/savebite/banhmi.png");
        firstReview.Reply.Should().NotBeNull();
        firstReview.Reply!.Content.Should().Be("Cam on ban da ung ho quan!");
        firstReview.Reply.RepliedByName.Should().Be("SaveBite Bakery Owner");

        _shopRepoMock.Verify(r => r.ShopExistsAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.GetPagedStoreReviewsAsync(_existingShopId, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Lọc đánh giá theo số sao (Rating = 5), review không có phản hồi (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetStoreReviews_FilterByRating_WithoutReplies_ShouldReturnFilteredReviews_Normal()
    {
        // Arrange
        var request = new StoreReviewsQueryRequest
        {
            Page = 1,
            PageSize = 10,
            Rating = 5
        };

        var feedbacks = CreateSampleFeedbacks().Take(1).ToList();
        feedbacks[0].Reply = null; // Khong co phan hoi
        var breakdown = new Dictionary<int, int> { { 5, 1 } };

        _shopRepoMock
            .Setup(r => r.ShopExistsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _shopRepoMock
            .Setup(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((5.0, 1, breakdown));

        _shopRepoMock
            .Setup(r => r.GetPagedStoreReviewsAsync(_existingShopId, 5, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((feedbacks, 1));

        // Act
        var result = await _service.GetStoreReviewsAsync(_existingShopId, request);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.AverageRating.Should().Be(5.0);
        result.TotalReviews.Should().Be(1);
        result.Reviews.Items.Should().HaveCount(1);
        result.Reviews.Items.First().Rating.Should().Be(5);
        result.Reviews.Items.First().Reply.Should().BeNull();

        _shopRepoMock.Verify(r => r.ShopExistsAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.GetPagedStoreReviewsAsync(_existingShopId, 5, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Quán chưa có đánh giá nào (danh sách rỗng) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetStoreReviews_EmptyReviews_ShouldReturnEmptySummary_Normal()
    {
        // Arrange
        var request = new StoreReviewsQueryRequest
        {
            Page = 1,
            PageSize = 10,
            Rating = null
        };

        _shopRepoMock
            .Setup(r => r.ShopExistsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _shopRepoMock
            .Setup(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((5.0, 0, new Dictionary<int, int>()));

        _shopRepoMock
            .Setup(r => r.GetPagedStoreReviewsAsync(_existingShopId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ProductFeedback>(), 0));

        // Act
        var result = await _service.GetStoreReviewsAsync(_existingShopId, request);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.AverageRating.Should().Be(5.0);
        result.TotalReviews.Should().Be(0);
        result.RatingBreakdown.Should().BeEmpty();
        result.Reviews.Items.Should().BeEmpty();
        result.Reviews.TotalItems.Should().Be(0);

        _shopRepoMock.Verify(r => r.ShopExistsAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID04: Quán không tồn tại trong hệ thống -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetStoreReviews_ShopNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new StoreReviewsQueryRequest
        {
            Page = 1,
            PageSize = 10,
            Rating = null
        };

        _shopRepoMock
            .Setup(r => r.ShopExistsAsync(_nonExistentShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.GetStoreReviewsAsync(_nonExistentShopId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop not found.");

        _shopRepoMock.Verify(r => r.GetShopReviewStatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _shopRepoMock.Verify(r => r.GetPagedStoreReviewsAsync(It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: Số sao Rating không hợp lệ (< 1 hoặc > 5, ví dụ: 6) -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetStoreReviews_InvalidRating_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new StoreReviewsQueryRequest
        {
            Page = 1,
            PageSize = 10,
            Rating = 6 // Rating > 5 không hợp lệ
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID05", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Rating" && e.ErrorMessage.Contains("Rating must be between 1 and 5 stars"));
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

    private void PrintValidationError(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 Bad Request");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }
}
