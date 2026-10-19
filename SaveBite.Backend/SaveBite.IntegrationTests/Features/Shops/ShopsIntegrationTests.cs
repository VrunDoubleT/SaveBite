using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;

namespace SaveBite.IntegrationTests.Features.Shops;

public class ShopsIntegrationTests : BaseIntegrationTest
{
    public ShopsIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User owner, Shop shop)> CreateActiveShopAsync(string shopName = "Bánh Mì Hà Nội")
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner_{Guid.NewGuid():N}@test.com",
            FullName = "Chủ Quán",
            PasswordHash = "dummyhash",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(owner);

        var appId = Guid.NewGuid();
        var application = new ShopApplication
        {
            Id = appId,
            ApplicantUserId = owner.Id,
            Name = shopName,
            AddressLine = "123 Phố Huế",
            Ward = "Hàng Bài",
            District = "Hoàn Kiếm",
            City = "Hà Nội",
            Latitude = 21.0285,
            Longitude = 105.8542,
            BankName = "Vietcombank",
            BankAccountNumber = "123456789",
            BankAccountHolder = "Chu Quan",
            PayosClientId = "cid",
            PayosApiKey = "key",
            PayosChecksumKey = "checksum",
            Status = ShopApplicationStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ShopApplications.Add(application);

        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            OwnerUserId = owner.Id,
            ApplicationId = appId,
            Name = shopName,
            Description = "Cửa hàng bánh mì nóng giòn",
            AddressLine = "123 Phố Huế",
            Ward = "Hàng Bài",
            District = "Hoàn Kiếm",
            City = "Hà Nội",
            Latitude = 21.0285,
            Longitude = 105.8542,
            OpeningTime = new TimeOnly(6, 0),
            ClosingTime = new TimeOnly(22, 0),
            Status = ShopStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Shops.Add(shop);
        await DbContext.SaveChangesAsync();

        return (owner, shop);
    }

    [Fact]
    public async Task GetNearby_WhenCalled_ReturnsPagedResult_200()
    {
        // Arrange: Tạo 1 cửa hàng tại Hà Nội
        var (_, shop) = await CreateActiveShopAsync("Tiệm Trà Sữa");

        // Act: Tìm kiếm xung quanh tọa độ gần đó (tối đa 10km)
        var response = await Client.GetAsync("/api/shops?latitude=21.0285&longitude=105.8542&radiusInKm=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<NearbyShopResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().Contain(s => s.Id == shop.Id);
    }

    [Fact]
    public async Task GetNearby_WithKeywordFilter_FiltersCorrectly_200()
    {
        // Arrange
        await CreateActiveShopAsync("Bánh Mì Pate Cột Đèn");
        await CreateActiveShopAsync("Trà Sữa Trân Châu");

        // Act: Tìm kiếm với từ khóa "Pate"
        var response = await Client.GetAsync("/api/shops?latitude=21.0285&longitude=105.8542&keyword=Pate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<NearbyShopResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().OnlyContain(s => s.Name.Contains("Pate"));
    }

    [Fact]
    public async Task GetProfile_WhenShopExists_ReturnsProfile_200()
    {
        // Arrange
        var (_, shop) = await CreateActiveShopAsync("Tiệm Cà Phê Trứng");

        // Act
        var response = await Client.GetAsync($"/api/shops/{shop.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ShopProfileResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(shop.Id);
        body.Data.Name.Should().Be("Tiệm Cà Phê Trứng");
    }

    [Fact]
    public async Task GetProfile_WhenShopNotFound_ReturnsNotFound_404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/shops/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProfile_WhenIdIsEmptyGuid_ReturnsBadRequest_400()
    {
        // Act: Truyền Guid.Empty (00000000-0000-0000-0000-000000000000)
        var response = await Client.GetAsync($"/api/shops/{Guid.Empty}");

        // Assert: 400 Bad Request ("Shop ID is invalid.")
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReviews_WhenShopExists_ReturnsReviewsSummary_200()
    {
        // Arrange
        var (_, shop) = await CreateActiveShopAsync("Tiệm Pizza Ý");

        // Act
        var response = await Client.GetAsync($"/api/shops/{shop.Id}/reviews");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreReviewsSummaryResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
    }
}
