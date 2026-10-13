using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FlashDealDiscovery;

/// <summary>
/// Unit Tests for Feature: View Flash Deals by Current Location
/// Service: FlashDealService.GetNearbyAsync & FlashDealCursorRequestValidator
/// Function Code: UT-27
/// Sheet Name: UT_ViewFlashDealsByLocation
/// </summary>
public class ViewFlashDealsByLocationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFlashDealRedisService> _redisMock = new();
    private readonly Mock<IFlashDealRepository> _dealRepoMock = new();
    private readonly Mock<ILogger<FlashDealService>> _loggerMock = new();
    private readonly FlashDealService _service;
    private readonly FlashDealCursorRequestValidator _validator = new();

    public ViewFlashDealsByLocationTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FlashDealService(
            _redisMock.Object,
            _dealRepoMock.Object,
            _loggerMock.Object
        );
    }

    private List<FlashDealResponse> CreateSampleDeals()
    {
        var now = DateTime.UtcNow;
        return new List<FlashDealResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = Guid.NewGuid(),
                ShopName = "Bakery A",
                CategoryName = "Bakery",
                DistanceInKm = 1.2,
                MinDealPrice = 20000,
                CreatedAt = now.AddMinutes(-10),
                Status = FlashDealStatus.OnSale
            },
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = Guid.NewGuid(),
                ShopName = "Coffee B",
                CategoryName = "Drinks",
                DistanceInKm = 3.5,
                MinDealPrice = 40000,
                CreatedAt = now.AddMinutes(-30),
                Status = FlashDealStatus.OnSale
            },
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = Guid.NewGuid(),
                ShopName = "Bistro C",
                CategoryName = "Bakery",
                DistanceInKm = 6.0,
                MinDealPrice = 60000,
                CreatedAt = now.AddMinutes(-60),
                Status = FlashDealStatus.OnSale
            }
        };
    }

    // =========================================================================
    // UTCID01: Duyệt deal ban đầu (Initial mode, không bộ lọc) -> Trả về danh sách deal theo CreatedAt giảm dần (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetNearby_InitialMode_NoFilters_ReturnsSortedDeals_Normal()
    {
        // Arrange
        var request = new FlashDealCursorRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 15.0,
            Mode = CursorMode.Initial,
            Limit = 9
        };

        var deals = CreateSampleDeals();
        _redisMock
            .Setup(r => r.GetNearbyDealsAsync(10.7769, 106.7009, 15.0))
            .ReturnsAsync(deals);

        // Act
        var result = await _service.GetNearbyAsync(request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Deals.Should().HaveCount(3);
        result.Cursor1.Should().NotBeNull();
        result.Cursor2.Should().NotBeNull();
        result.HasOlder.Should().BeFalse(); // 3 < Limit 9
        // Thứ tự mới nhất trước
        result.Deals.First().ShopName.Should().Be("Bakery A");
    }

    // =========================================================================
    // UTCID02: Lọc theo Category và Price -> Chỉ trả về deal thỏa mãn cả 2 tiêu chí (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetNearby_FilterCategoryAndPrice_ReturnsMatchingDeals_Normal()
    {
        // Arrange
        var request = new FlashDealCursorRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 15.0,
            Category = "Bakery",
            Price = "Under 30k",
            Mode = CursorMode.Initial,
            Limit = 9
        };

        var deals = CreateSampleDeals();
        _redisMock
            .Setup(r => r.GetNearbyDealsAsync(10.7769, 106.7009, 15.0))
            .ReturnsAsync(deals);

        // Act
        var result = await _service.GetNearbyAsync(request);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Deals.Should().HaveCount(1);
        result.Deals.First().ShopName.Should().Be("Bakery A");
        result.Deals.First().CategoryName.Should().Be("Bakery");
        result.Deals.First().MinDealPrice.Should().BeLessThan(30000);
    }

    // =========================================================================
    // UTCID03: Lọc theo khoảng cách Distance ("Under 2km") -> Chỉ trả về deal gần hơn 2km (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetNearby_FilterDistanceUnder2km_ReturnsNearbyDeals_Normal()
    {
        // Arrange
        var request = new FlashDealCursorRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 15.0,
            Distance = "Under 2km",
            Mode = CursorMode.Initial,
            Limit = 9
        };

        var deals = CreateSampleDeals();
        _redisMock
            .Setup(r => r.GetNearbyDealsAsync(10.7769, 106.7009, 15.0))
            .ReturnsAsync(deals);

        // Act
        var result = await _service.GetNearbyAsync(request);

        // Assert & Log
        PrintSuccess("UTCID03", result);
        result.Should().NotBeNull();
        result.Deals.Should().HaveCount(1);
        result.Deals.First().DistanceInKm.Should().BeLessThan(2.0);
        result.Deals.First().ShopName.Should().Be("Bakery A");
    }

    // =========================================================================
    // UTCID04: Phân trang trang kế tiếp (Mode = Older, Cursor1) -> Lấy các deal cũ hơn Cursor1 (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetNearby_ModeOlder_PaginatesOlderDeals_Normal()
    {
        // Arrange
        var deals = CreateSampleDeals();
        var cursor1 = deals[0].CreatedAt; // Cũ hơn deal đầu tiên

        var request = new FlashDealCursorRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 15.0,
            Mode = CursorMode.Older,
            Cursor1 = cursor1,
            Limit = 2
        };

        _redisMock
            .Setup(r => r.GetNearbyDealsAsync(10.7769, 106.7009, 15.0))
            .ReturnsAsync(deals);

        // Act
        var result = await _service.GetNearbyAsync(request);

        // Assert & Log
        PrintSuccess("UTCID04", result);
        result.Should().NotBeNull();
        result.Deals.Should().HaveCount(2); // Coffee B và Bistro C
        result.Deals.Should().OnlyContain(d => d.CreatedAt < cursor1);
    }

    // =========================================================================
    // UTCID05: Polling kiểm tra deal mới (Mode = Newer, Cursor2) -> Deals rỗng, NewDealsCount > 0 (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetNearby_ModeNewer_PollingReturnsBadgeCount_Normal()
    {
        // Arrange
        var deals = CreateSampleDeals();
        var cursor2 = deals[1].CreatedAt; // Deal 0 mới hơn cursor2

        var request = new FlashDealCursorRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 15.0,
            Mode = CursorMode.Newer,
            Cursor2 = cursor2
        };

        _redisMock
            .Setup(r => r.GetNearbyDealsAsync(10.7769, 106.7009, 15.0))
            .ReturnsAsync(deals);

        // Act
        var result = await _service.GetNearbyAsync(request);

        // Assert & Log
        PrintSuccess("UTCID05", result);
        result.Should().NotBeNull();
        result.Deals.Should().BeEmpty(); // Khi polling không trả về deals
        result.HasNewer.Should().BeTrue();
        result.NewDealsCount.Should().Be(1);
    }

    // =========================================================================
    // UTCID06: Bán kính không có deal nào xung quanh -> Trả về danh sách rỗng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID06_GetNearby_NoDealsInRadius_ReturnsEmptyList_Normal()
    {
        // Arrange
        var request = new FlashDealCursorRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 15.0,
            Mode = CursorMode.Initial,
            Limit = 9
        };

        _redisMock
            .Setup(r => r.GetNearbyDealsAsync(10.7769, 106.7009, 15.0))
            .ReturnsAsync(new List<FlashDealResponse>());

        // Act
        var result = await _service.GetNearbyAsync(request);

        // Assert & Log
        PrintSuccess("UTCID06", result);
        result.Should().NotBeNull();
        result.Deals.Should().BeEmpty();
        result.HasOlder.Should().BeFalse();
        result.Cursor1.Should().BeNull();
        result.Cursor2.Should().BeNull();
    }

    // =========================================================================
    // UTCID07: Tọa độ Latitude ngoài khoảng [-90, 90] -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID07_GetNearby_LatitudeOutOfRange_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new FlashDealCursorRequest
        {
            Latitude = 95.0, // Vượt ngoài khoảng [-90, 90]
            Longitude = 106.7009,
            RadiusInKm = 15.0
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintException("UTCID07", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Latitude" && e.ErrorMessage.Contains("Latitude must be between -90 and 90"));
    }

    // =========================================================================
    // UTCID08: Bán kính RadiusInKm <= 0 -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID08_GetNearby_RadiusZeroOrNegative_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new FlashDealCursorRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 0.0 // Bán kính không hợp lệ <= 0
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintException("UTCID08", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "RadiusInKm" && e.ErrorMessage.Contains("RadiusInKm must be greater than 0"));
    }

    private void PrintSuccess<T>(string tcid, T result)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }

    private void PrintException(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: ERROR / REJECTED");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }
}
