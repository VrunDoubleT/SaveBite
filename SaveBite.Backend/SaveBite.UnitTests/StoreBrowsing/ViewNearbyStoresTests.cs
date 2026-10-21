using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Moq;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreBrowsing;

/// <summary>
/// Unit Tests for Feature: View Nearby Stores
/// Service: ShopService.GetNearbyShopsAsync & NearbyShopsRequestValidator
/// Function Code: UT-29
/// Sheet Name: UT_ViewNearbyStores
/// </summary>
public class ViewNearbyStoresTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock = new();
    private readonly ShopService _service;
    private readonly NearbyShopsRequestValidator _validator = new();

    public ViewNearbyStoresTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopService(_shopRepoMock.Object, _userAddressRepoMock.Object, _cloudinaryServiceMock.Object);
    }

    private List<Shop> CreateSampleShops()
    {
        return new List<Shop>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "SaveBite Bakery 1",
                Description = "Fresh bread and pastries",
                AddressLine = "123 Le Loi Street",
                District = "District 1",
                City = "Ho Chi Minh City",
                Latitude = 10.7769, // Rất gần vị trí user (~0 km)
                Longitude = 106.7009,
                OpeningTime = new TimeOnly(6, 0),
                ClosingTime = new TimeOnly(23, 0),
                LogoUrl = "https://res.cloudinary.com/savebite/logo1.png"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "SaveBite Cafe 2",
                Description = "Coffee and snacks",
                AddressLine = "456 Nguyen Hue Boulevard",
                District = "District 1",
                City = "Ho Chi Minh City",
                Latitude = 10.7745, // Cách ~0.3 km
                Longitude = 106.7025,
                OpeningTime = new TimeOnly(0, 0),
                ClosingTime = new TimeOnly(1, 0), // Đã đóng cửa ở giờ ban ngày
                LogoUrl = "https://res.cloudinary.com/savebite/logo2.png"
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "SaveBite Bistro 3",
                Description = "Dinner bistro",
                AddressLine = "789 Vo Van Tan",
                District = "District 3",
                City = "Ho Chi Minh City",
                Latitude = 10.7780, // Cách ~1.2 km
                Longitude = 106.6900,
                OpeningTime = new TimeOnly(6, 0),
                ClosingTime = new TimeOnly(23, 0),
                LogoUrl = "https://res.cloudinary.com/savebite/logo3.png"
            }
        };
    }

    // =========================================================================
    // UTCID01: Tìm kiếm quán gần vị trí hiện tại trong bán kính (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetNearbyShops_DefaultWithinRadius_ReturnsSortedShops_Normal()
    {
        // Arrange
        var request = new NearbyShopsRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 10.0,
            Keyword = null,
            OnlyOpen = false,
            Page = 1,
            PageSize = 6
        };

        var shops = CreateSampleShops();
        _shopRepoMock
            .Setup(r => r.GetActiveShopsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shops);

        var reviewsStats = new Dictionary<Guid, (double AvgRating, int TotalCount)>
        {
            { shops[0].Id, (4.8, 15) },
            { shops[1].Id, (4.5, 10) },
            { shops[2].Id, (5.0, 5) }
        };

        _shopRepoMock
            .Setup(r => r.GetReviewsStatsForShopsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewsStats);

        // Act
        var result = await _service.GetNearbyShopsAsync(request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalItems.Should().Be(3);
        // Sắp xếp theo khoảng cách tăng dần
        result.Items.First().Id.Should().Be(shops[0].Id);
        result.Items.First().AverageRating.Should().Be(4.8);
        result.Items.First().TotalReviews.Should().Be(15);
    }

    // =========================================================================
    // UTCID02: Lọc chỉ lấy quán đang mở cửa (OnlyOpen = true) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetNearbyShops_OnlyOpenTrue_FiltersOutClosedShops_Normal()
    {
        // Arrange
        var request = new NearbyShopsRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 10.0,
            OnlyOpen = true,
            Page = 1,
            PageSize = 6
        };

        var shops = CreateSampleShops();
        _shopRepoMock
            .Setup(r => r.GetActiveShopsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shops);

        _shopRepoMock
            .Setup(r => r.GetReviewsStatsForShopsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, (double AvgRating, int TotalCount)>());

        // Act
        var result = await _service.GetNearbyShopsAsync(request);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        // Quán số 2 đóng cửa (00:00 - 01:00) bị lọc bỏ
        result.Items.Should().OnlyContain(s => s.IsOpen);
    }

    // =========================================================================
    // UTCID03: Tìm kiếm quán theo từ khóa (Keyword = "Bakery") (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetNearbyShops_WithKeyword_ReturnsMatchingShops_Normal()
    {
        // Arrange
        var request = new NearbyShopsRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 10.0,
            Keyword = "Bakery",
            OnlyOpen = false,
            Page = 1,
            PageSize = 6
        };

        var shops = CreateSampleShops().Where(s => s.Name.Contains("Bakery")).ToList();
        _shopRepoMock
            .Setup(r => r.GetActiveShopsAsync("Bakery", It.IsAny<CancellationToken>()))
            .ReturnsAsync(shops);

        _shopRepoMock
            .Setup(r => r.GetReviewsStatsForShopsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, (double AvgRating, int TotalCount)>());

        // Act
        var result = await _service.GetNearbyShopsAsync(request);

        // Assert & Log
        PrintSuccess("UTCID03", result);
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Contain("Bakery");
    }

    // =========================================================================
    // UTCID04: Không có quán nào trong bán kính tìm kiếm (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetNearbyShops_NoShopsInRadius_ReturnsEmptyPagedResult_Normal()
    {
        // Arrange
        var request = new NearbyShopsRequest
        {
            Latitude = 10.0, // Vị trí xa
            Longitude = 105.0,
            RadiusInKm = 1.0, // Bán kính hẹp
            Page = 1,
            PageSize = 6
        };

        var shops = CreateSampleShops(); // Tọa độ ở TP.HCM (10.77, 106.70)
        _shopRepoMock
            .Setup(r => r.GetActiveShopsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shops);

        // Act
        var result = await _service.GetNearbyShopsAsync(request);

        // Assert & Log
        PrintSuccess("UTCID04", result);
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
    }

    // =========================================================================
    // UTCID05: Tọa độ Latitude ngoài khoảng [-90, 90] -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetNearbyShops_LatitudeOutOfRange_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new NearbyShopsRequest
        {
            Latitude = 95.0, // Vượt ngoài khoảng [-90, 90]
            Longitude = 106.7009,
            RadiusInKm = 10.0
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintException("UTCID05", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Latitude" && e.ErrorMessage.Contains("Latitude must be between -90 and 90"));
    }

    // =========================================================================
    // UTCID06: Bán kính RadiusInKm <= 0 -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID06_GetNearbyShops_RadiusZeroOrNegative_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new NearbyShopsRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 0.0 // Bán kính không hợp lệ <= 0
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintException("UTCID06", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "RadiusInKm" && e.ErrorMessage.Contains("RadiusInKm must be greater than 0"));
    }

    // =========================================================================
    // UTCID07: Từ khóa tìm kiếm Keyword vượt quá 100 ký tự -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID07_GetNearbyShops_KeywordExceedsMaxLength_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new NearbyShopsRequest
        {
            Latitude = 10.7769,
            Longitude = 106.7009,
            RadiusInKm = 10.0,
            Keyword = new string('A', 105) // 105 ký tự > 100
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintException("UTCID07", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Keyword" && e.ErrorMessage.Contains("Keyword must not exceed 100 characters"));
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
