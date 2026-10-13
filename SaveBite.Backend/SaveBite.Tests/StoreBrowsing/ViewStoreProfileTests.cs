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

namespace SaveBite.Tests.StoreBrowsing;

/// <summary>
/// Unit Tests for Feature: View Store Profile
/// Service: ShopService.GetShopProfileAsync
/// Function Code: UT-30
/// Sheet Name: UT_ViewStoreProfile
/// </summary>
public class ViewStoreProfileTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly ShopService _service;

    // Fixed Test Identifiers
    private readonly Guid _existingShopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _nonExistentShopId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public ViewStoreProfileTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopService(_shopRepoMock.Object);
    }

    private Shop CreateValidShop(TimeOnly? openingTime = null, TimeOnly? closingTime = null)
    {
        return new Shop
        {
            Id = _existingShopId,
            Name = "SaveBite Bakery",
            Description = "Delicious bakery items",
            AddressLine = "123 Le Loi Street",
            Ward = "Ben Nghe",
            District = "District 1",
            City = "Ho Chi Minh City",
            Latitude = 10.7769,
            Longitude = 106.7009,
            LogoUrl = "https://res.cloudinary.com/savebite/logo.png",
            CoverImageUrl = "https://res.cloudinary.com/savebite/cover.jpg",
            OpeningTime = openingTime,
            ClosingTime = closingTime,
            Status = ShopStatus.Active
        };
    }

    // =========================================================================
    // UTCID01: Xem hồ sơ quán đang mở cửa, có đầy đủ đánh giá (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetShopProfile_ActiveAndOpen_HasReviews_ShouldReturnProfile_Normal()
    {
        // Arrange
        var now = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var shop = CreateValidShop(now.AddHours(-2), now.AddHours(2));
        var breakdown = new Dictionary<int, int> { { 5, 10 }, { 4, 5 } };

        _shopRepoMock
            .Setup(r => r.GetActiveShopByIdAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _shopRepoMock
            .Setup(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((4.8, 15, breakdown));

        // Act
        var result = await _service.GetShopProfileAsync(_existingShopId);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingShopId);
        result.Name.Should().Be("SaveBite Bakery");
        result.Description.Should().Be("Delicious bakery items");
        result.AddressLine.Should().Be("123 Le Loi Street");
        result.Ward.Should().Be("Ben Nghe");
        result.District.Should().Be("District 1");
        result.City.Should().Be("Ho Chi Minh City");
        result.Latitude.Should().Be(10.7769);
        result.Longitude.Should().Be(106.7009);
        result.LogoUrl.Should().Be("https://res.cloudinary.com/savebite/logo.png");
        result.CoverImageUrl.Should().Be("https://res.cloudinary.com/savebite/cover.jpg");
        result.IsOpen.Should().BeTrue();
        result.AverageRating.Should().Be(4.8);
        result.TotalReviews.Should().Be(15);
        result.RatingBreakdown.Should().ContainKey(5).WhoseValue.Should().Be(10);
        result.RatingBreakdown.Should().ContainKey(4).WhoseValue.Should().Be(5);

        _shopRepoMock.Verify(r => r.GetActiveShopByIdAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Xem hồ sơ quán đang đóng cửa, chưa có đánh giá nào (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetShopProfile_ActiveAndClosed_NoReviews_ShouldReturnProfile_Normal()
    {
        // Arrange
        var now = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var shop = CreateValidShop(now.AddHours(2), now.AddHours(4));
        var emptyBreakdown = new Dictionary<int, int>();

        _shopRepoMock
            .Setup(r => r.GetActiveShopByIdAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _shopRepoMock
            .Setup(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((0.0, 0, emptyBreakdown));

        // Act
        var result = await _service.GetShopProfileAsync(_existingShopId);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingShopId);
        result.IsOpen.Should().BeFalse();
        result.AverageRating.Should().Be(0.0);
        result.TotalReviews.Should().Be(0);
        result.RatingBreakdown.Should().BeEmpty();

        _shopRepoMock.Verify(r => r.GetActiveShopByIdAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Quán không cấu hình giờ mở/đóng cửa (null) -> Mặc định IsOpen = true (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetShopProfile_NullOperatingHours_ShouldDefaultIsOpenTrue_Normal()
    {
        // Arrange
        var shop = CreateValidShop(openingTime: null, closingTime: null);

        _shopRepoMock
            .Setup(r => r.GetActiveShopByIdAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _shopRepoMock
            .Setup(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((4.8, 15, new Dictionary<int, int>()));

        // Act
        var result = await _service.GetShopProfileAsync(_existingShopId);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.OpeningTime.Should().BeNull();
        result.ClosingTime.Should().BeNull();
        result.IsOpen.Should().BeTrue();

        _shopRepoMock.Verify(r => r.GetActiveShopByIdAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID04: Quán không có thông tin tùy chọn (Description, LogoUrl, CoverImageUrl, Ward, District, City == null) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetShopProfile_OptionalFieldsNull_ShouldReturnProfileSuccessfully_Normal()
    {
        // Arrange
        var shop = new Shop
        {
            Id = _existingShopId,
            Name = "SaveBite Mini Kiosk",
            Description = null,
            AddressLine = "999 Nguyen Trai",
            Ward = null,
            District = null,
            City = null,
            Latitude = 10.7600,
            Longitude = 106.6800,
            LogoUrl = null,
            CoverImageUrl = null,
            OpeningTime = null,
            ClosingTime = null,
            Status = ShopStatus.Active
        };

        _shopRepoMock
            .Setup(r => r.GetActiveShopByIdAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _shopRepoMock
            .Setup(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((5.0, 0, new Dictionary<int, int>()));

        // Act
        var result = await _service.GetShopProfileAsync(_existingShopId);

        // Assert & Log
        PrintSuccess("UTCID04", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingShopId);
        result.Name.Should().Be("SaveBite Mini Kiosk");
        result.Description.Should().BeNull();
        result.Ward.Should().BeNull();
        result.District.Should().BeNull();
        result.City.Should().BeNull();
        result.LogoUrl.Should().BeNull();
        result.CoverImageUrl.Should().BeNull();
        result.IsOpen.Should().BeTrue();

        _shopRepoMock.Verify(r => r.GetActiveShopByIdAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.GetShopReviewStatsAsync(_existingShopId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID05: Quán không tồn tại hoặc không ở trạng thái Active -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetShopProfile_ShopNotFoundOrInactive_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _shopRepoMock
            .Setup(r => r.GetActiveShopByIdAsync(_nonExistentShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var act = () => _service.GetShopProfileAsync(_nonExistentShopId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop not found or is currently inactive.");

        _shopRepoMock.Verify(r => r.GetShopReviewStatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
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
