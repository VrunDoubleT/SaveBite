using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;

namespace SaveBite.IntegrationTests.Features.FlashDeals;

public class FlashDealsIntegrationTests : BaseIntegrationTest
{
    public FlashDealsIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User owner, Shop shop, Product product, ProductVariant variant, FlashDeal deal)> SetupStoreOwnerScenarioAsync(
        FlashDealStatus dealStatus = FlashDealStatus.OnSale,
        bool createDeal = true)
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@test.com",
            fullName: "Flash Deal Owner");

        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);
        shop.Latitude = 21.0285;
        shop.Longitude = 105.8542;

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
            Name = $"Bánh Mì Flash Deal {Guid.NewGuid():N}",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = $"FD-{Random.Shared.Next(1000, 9999)}",
            OriginalPrice = 50000m,
            DealPrice = 25000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        product.Variants.Add(variant);
        DbContext.Products.Add(product);

        FlashDeal deal = null!;
        if (createDeal)
        {
            var now = DateTime.UtcNow;
            deal = new FlashDeal
            {
                Id = Guid.NewGuid(),
                ShopId = shop.Id,
                ProductId = product.Id,
                SaleStartTime = dealStatus == FlashDealStatus.Pending ? now.AddMinutes(20) : now.AddMinutes(-30),
                OrderEndTime = dealStatus == FlashDealStatus.Pending ? now.AddMinutes(80) : now.AddMinutes(30),
                ShopClosingTime = now.AddHours(4),
                Status = dealStatus,
                CreatedAt = now,
                UpdatedAt = now
            };

            var dealVariant = new FlashDealVariant
            {
                Id = Guid.NewGuid(),
                FlashDealId = deal.Id,
                VariantId = variant.Id,
                OriginalPrice = 50000m,
                DealPrice = 25000m,
                DiscountPercent = 50,
                TotalQuantity = 10,
                ReservedQuantity = 0,
                SoldQuantity = 0,
                Status = FlashDealVariantStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };
            deal.Variants.Add(dealVariant);
            DbContext.FlashDeals.Add(deal);
        }

        await DbContext.SaveChangesAsync();
        SetBearerToken(owner.Id, owner.Email, owner.Role);

        return (owner, shop, product, variant, deal);
    }

    private async Task<(Shop shop, Product product, FlashDeal deal)> SetupActiveFlashDealAsync()
    {
        var (_, shop, product, _, deal) = await SetupStoreOwnerScenarioAsync();
        return (shop, product, deal);
    }

    // === GUEST ACCESS ENDPOINTS ===

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
        var (_, product, deal) = await SetupActiveFlashDealAsync();

        // Act
        var response = await Client.GetAsync($"/api/flash-deals/{deal.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<FlashDealResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(deal.Id);
        body.Data.ProductName.Should().Be(product.Name);
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

    // === STORE OWNER MANAGEMENT ENDPOINTS ===

    [Fact]
    public async Task GetShopDeals_WhenStoreOwnerAuthenticated_ReturnsPagedDeals_200()
    {
        // Arrange
        var (_, _, product, _, deal) = await SetupStoreOwnerScenarioAsync();

        // Act
        var response = await Client.GetAsync("/api/flash-deals/shop?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ShopFlashDealListItemResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().Contain(d => d.Id == deal.Id && d.ProductId == product.Id);
    }

    [Fact]
    public async Task GetShopDealDetail_WhenDealExists_ReturnsShopDetail_200()
    {
        // Arrange
        var (_, _, product, _, deal) = await SetupStoreOwnerScenarioAsync();

        // Act
        var response = await Client.GetAsync($"/api/flash-deals/shop/{deal.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopFlashDealDetailResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Id.Should().Be(deal.Id);
        result.Data.ProductId.Should().Be(product.Id);
        result.Data.Variants.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetShopDealDetail_WhenNotOwnedByShop_ReturnsNotFound_404()
    {
        // Arrange: First owner owns deal
        var (_, _, _, _, deal) = await SetupStoreOwnerScenarioAsync();

        // Second owner authenticated
        var otherOwner = await AuthenticateAsStoreOwnerAsync(
            email: $"other_owner_{Guid.NewGuid():N}@test.com",
            fullName: "Other Owner");

        // Act
        var response = await Client.GetAsync($"/api/flash-deals/shop/{deal.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEligibleProducts_WhenActiveShop_ReturnsEligibleProductsList_200()
    {
        // Arrange: Create shop and product without any active deal
        var (_, _, product, _, _) = await SetupStoreOwnerScenarioAsync(createDeal: false);

        // Act
        var response = await Client.GetAsync("/api/flash-deals/eligible-products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ShopEligibleProductResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().Contain(p => p.Id == product.Id);
    }

    [Fact]
    public async Task Create_WhenImmediate_CreatesActiveFlashDeal_201()
    {
        // Arrange
        var (_, shop, product, variant, _) = await SetupStoreOwnerScenarioAsync(createDeal: false);

        var now = DateTime.UtcNow;
        var request = new CreateFlashDealRequest
        {
            ProductId = product.Id,
            IsImmediate = true,
            OrderEndTime = now.AddMinutes(60),
            ShopClosingTime = now.AddHours(2),
            Variants = new List<CreateFlashDealVariantRequest>
            {
                new()
                {
                    VariantId = variant.Id,
                    DealPrice = 20000m,
                    TotalQuantity = 15
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/flash-deals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopFlashDealDetailResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Status.Should().Be(FlashDealStatus.OnSale);
        result.Data.Variants.Should().Contain(v => v.VariantId == variant.Id && v.DealPrice == 20000m);

        // Verify in DB
        var dealInDb = await DbContext.FlashDeals
            .Include(d => d.Variants)
            .AsNoTracking()
            .SingleAsync(d => d.Id == result.Data.Id);
        dealInDb.ShopId.Should().Be(shop.Id);
        dealInDb.Status.Should().Be(FlashDealStatus.OnSale);
    }

    [Fact]
    public async Task Create_WhenScheduled_CreatesPendingFlashDeal_201()
    {
        // Arrange
        var (_, _, product, variant, _) = await SetupStoreOwnerScenarioAsync(createDeal: false);

        var now = DateTime.UtcNow;
        var start = now.AddMinutes(20);
        var end = start.AddMinutes(45);
        var request = new CreateFlashDealRequest
        {
            ProductId = product.Id,
            IsImmediate = false,
            SaleStartTime = start,
            OrderEndTime = end,
            ShopClosingTime = end.AddHours(1),
            Variants = new List<CreateFlashDealVariantRequest>
            {
                new()
                {
                    VariantId = variant.Id,
                    DealPrice = 30000m,
                    TotalQuantity = 10
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/flash-deals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopFlashDealDetailResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Status.Should().Be(FlashDealStatus.Pending);
        result.Data.Variants.Should().Contain(v => v.VariantId == variant.Id && v.DealPrice == 30000m);
    }

    [Fact]
    public async Task Create_WhenDealPriceHigherThanOriginal_ReturnsBadRequest_400()
    {
        // Arrange: Original price is 50000m, Deal price is 60000m
        var (_, _, product, variant, _) = await SetupStoreOwnerScenarioAsync(createDeal: false);

        var now = DateTime.UtcNow;
        var request = new CreateFlashDealRequest
        {
            ProductId = product.Id,
            IsImmediate = true,
            OrderEndTime = now.AddMinutes(45),
            ShopClosingTime = now.AddHours(2),
            Variants = new List<CreateFlashDealVariantRequest>
            {
                new()
                {
                    VariantId = variant.Id,
                    DealPrice = 60000m,
                    TotalQuantity = 10
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/flash-deals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WhenPendingDeal_UpdatesTimesAndVariants_200()
    {
        // Arrange
        var (_, _, product, variant, deal) = await SetupStoreOwnerScenarioAsync(dealStatus: FlashDealStatus.Pending);

        var now = DateTime.UtcNow;
        var start = now.AddMinutes(25);
        var end = start.AddMinutes(50);
        var request = new UpdateFlashDealRequest
        {
            SaleStartTime = start,
            OrderEndTime = end,
            ShopClosingTime = end.AddHours(1),
            Variants = new List<UpdateFlashDealVariantRequest>
            {
                new()
                {
                    VariantId = variant.Id,
                    DealPrice = 18000m,
                    TotalQuantity = 20,
                    Status = FlashDealVariantStatus.Active
                }
            }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/flash-deals/{deal.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopFlashDealDetailResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Variants.Should().Contain(v => v.VariantId == variant.Id && v.DealPrice == 18000m && v.TotalQuantity == 20);

        // Verify in DB
        var dealInDb = await DbContext.FlashDeals
            .Include(d => d.Variants)
            .AsNoTracking()
            .SingleAsync(d => d.Id == deal.Id);
        dealInDb.Variants.First().TotalQuantity.Should().Be(20);
        dealInDb.Variants.First().DealPrice.Should().Be(18000m);
    }

    [Fact]
    public async Task Cancel_WhenActiveDeal_UpdatesStatusToCancelled_200()
    {
        // Arrange
        var (_, _, _, _, deal) = await SetupStoreOwnerScenarioAsync(dealStatus: FlashDealStatus.OnSale);

        // Act
        var response = await Client.PostAsync($"/api/flash-deals/{deal.Id}/cancellations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in DB
        var dealInDb = await DbContext.FlashDeals.AsNoTracking().SingleAsync(d => d.Id == deal.Id);
        dealInDb.Status.Should().Be(FlashDealStatus.Cancelled);
    }

    [Fact]
    public async Task Hide_WhenActiveDeal_UpdatesStatusToHidden_200()
    {
        // Arrange
        var (_, _, _, _, deal) = await SetupStoreOwnerScenarioAsync(dealStatus: FlashDealStatus.OnSale);

        // Act
        var response = await Client.PostAsync($"/api/flash-deals/{deal.Id}/hides", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in DB
        var dealInDb = await DbContext.FlashDeals.AsNoTracking().SingleAsync(d => d.Id == deal.Id);
        dealInDb.Status.Should().Be(FlashDealStatus.Hidden);
    }

    [Fact]
    public async Task Unhide_WhenHiddenDeal_UpdatesStatusToOnSale_200()
    {
        // Arrange
        var (_, _, _, _, deal) = await SetupStoreOwnerScenarioAsync(dealStatus: FlashDealStatus.Hidden);

        // Act
        var response = await Client.DeleteAsync($"/api/flash-deals/{deal.Id}/hides");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in DB
        var dealInDb = await DbContext.FlashDeals.AsNoTracking().SingleAsync(d => d.Id == deal.Id);
        dealInDb.Status.Should().Be(FlashDealStatus.OnSale);
    }

    // === AUTHORIZATION & PERMISSION TESTS ===

    [Fact]
    public async Task StoreOwnerEndpoints_WhenCustomerRole_ReturnsForbidden_403()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync(
            email: $"cust_{Guid.NewGuid():N}@test.com",
            fullName: "Regular Customer");

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act & Assert 1: GET /api/flash-deals/shop
        var getDealsResponse = await Client.GetAsync("/api/flash-deals/shop");
        getDealsResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act & Assert 2: GET /api/flash-deals/eligible-products
        var getEligibleResponse = await Client.GetAsync("/api/flash-deals/eligible-products");
        getEligibleResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act & Assert 3: POST /api/flash-deals
        var createResponse = await Client.PostAsJsonAsync("/api/flash-deals", new CreateFlashDealRequest());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StoreOwnerEndpoints_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act & Assert 1: GET /api/flash-deals/shop
        var getDealsResponse = await Client.GetAsync("/api/flash-deals/shop");
        getDealsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act & Assert 2: POST /api/flash-deals
        var createResponse = await Client.PostAsJsonAsync("/api/flash-deals", new CreateFlashDealRequest());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
