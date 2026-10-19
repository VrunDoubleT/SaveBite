using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;

namespace SaveBite.IntegrationTests.Features.FlashDeals;

public class FlashDealsIntegrationTests : BaseIntegrationTest
{
    public FlashDealsIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(Shop shop, Product product, FlashDeal deal)> SetupActiveFlashDealAsync()
    {
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner_{Guid.NewGuid():N}@test.com",
            FullName = "Flash Deal Owner",
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
            Name = "Flash Bakery",
            AddressLine = "123 Deal St",
            BankName = "Bank",
            BankAccountNumber = "123",
            BankAccountHolder = "Holder",
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
            Name = "Flash Bakery",
            AddressLine = "123 Deal St",
            Latitude = 21.0285,
            Longitude = 105.8542,
            Status = ShopStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Shops.Add(shop);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = $"Category_{Guid.NewGuid():N}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Bánh Mì Flash Deal",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "FD-01",
            OriginalPrice = 50000,
            DealPrice = 25000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        product.Variants.Add(variant);
        DbContext.Products.Add(product);

        var now = DateTime.UtcNow;
        var deal = new FlashDeal
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            ProductId = product.Id,
            SaleStartTime = now.AddHours(-1),
            OrderEndTime = now.AddHours(3),
            ShopClosingTime = now.AddHours(4),
            Status = FlashDealStatus.OnSale,
            CreatedAt = now,
            UpdatedAt = now
        };

        var dealVariant = new FlashDealVariant
        {
            Id = Guid.NewGuid(),
            FlashDealId = deal.Id,
            VariantId = variant.Id,
            OriginalPrice = 50000,
            DealPrice = 25000,
            DiscountPercent = 50,
            TotalQuantity = 10,
            ReservedQuantity = 0,
            SoldQuantity = 0,
            Status = FlashDealVariantStatus.Active
        };
        deal.Variants.Add(dealVariant);
        DbContext.FlashDeals.Add(deal);

        await DbContext.SaveChangesAsync();
        return (shop, product, deal);
    }

    [Fact]
    public async Task GetNearby_WhenCalled_ReturnsFlashDealsList_200()
    {
        // Arrange
        var (_, _, deal) = await SetupActiveFlashDealAsync();

        // Act
        var response = await Client.GetAsync("/api/flash-deals?latitude=21.0285&longitude=105.8542&radiusInKm=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<FlashDealCursorListResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Deals.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_WhenDealExists_ReturnsDealDetails_200()
    {
        // Arrange
        var (_, _, deal) = await SetupActiveFlashDealAsync();

        // Act
        var response = await Client.GetAsync($"/api/flash-deals/{deal.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<FlashDealResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(deal.Id);
        body.Data.ProductName.Should().Be("Bánh Mì Flash Deal");
    }

    [Fact]
    public async Task GetById_WhenDealNotFound_ReturnsNotFound_404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/flash-deals/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WhenIdIsEmpty_ReturnsBadRequest_400()
    {
        // Act
        var response = await Client.GetAsync($"/api/flash-deals/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByShop_WhenShopExists_ReturnsShopDeals_200()
    {
        // Arrange
        var (shop, _, deal) = await SetupActiveFlashDealAsync();

        // Act
        var response = await Client.GetAsync($"/api/shops/{shop.Id}/flash-deals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<FlashDealResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Should().Contain(d => d.Id == deal.Id);
    }

    [Fact]
    public async Task GetByShop_WhenShopIdIsEmpty_ReturnsBadRequest_400()
    {
        // Act
        var response = await Client.GetAsync($"/api/shops/{Guid.Empty}/flash-deals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
