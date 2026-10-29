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
using Xunit.Abstractions;

namespace SaveBite.IntegrationTests.Features.Orders;

public class CartIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public CartIntegrationTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    private async Task<(User customer, Shop shop, FlashDealVariant dealVariant)> SetupCustomerAndFlashDealVariantAsync()
    {
        var customer = await AuthenticateAsCustomerAsync(
            email: $"customer_{Guid.NewGuid():N}@test.com",
            fullName: "Test Customer");

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = $"owner_{Guid.NewGuid():N}@test.com",
            Phone = "0908888888",
            FullName = "Cart Shop Owner",
            PasswordHash = "dummy-hash",
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
            Name = "Cart Bakery",
            AddressLine = "456 Cart St",
            BankName = "CartBank",
            BankAccountNumber = "9876543210",
            BankAccountHolder = "Cart Owner",
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
            Name = "Cart Bakery",
            AddressLine = "456 Cart St",
            Latitude = 10.7769,
            Longitude = 106.7009,
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
            Name = "Bánh Mì Cart Test",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "CART-01",
            OriginalPrice = 40000,
            DealPrice = 20000,
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
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(3),
            Status = FlashDealStatus.OnSale,
            CreatedAt = now,
            UpdatedAt = now
        };

        var dealVariant = new FlashDealVariant
        {
            Id = Guid.NewGuid(),
            FlashDealId = deal.Id,
            VariantId = variant.Id,
            OriginalPrice = 40000,
            DealPrice = 20000,
            DiscountPercent = 50,
            TotalQuantity = 10,
            ReservedQuantity = 0,
            SoldQuantity = 0,
            Status = FlashDealVariantStatus.Active
        };
        deal.Variants.Add(dealVariant);
        DbContext.FlashDeals.Add(deal);

        await DbContext.SaveChangesAsync();

        return (customer, shop, dealVariant);
    }

    [Fact]
    public async Task GetCartSummary_WhenCartIsEmpty_ReturnsOkWithZeroTotals()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/cart/summary");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CartSummaryResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] GET /api/cart/summary: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalQuantity.Should().Be(0);
        result.Data.TotalFlashDeals.Should().Be(0);
    }

    [Fact]
    public async Task AddItemToCart_WithValidFlashDealVariant_ReturnsOkAndItemAdded()
    {
        // Arrange
        var (customer, _, dealVariant) = await SetupCustomerAndFlashDealVariantAsync();
        var request = new AddCartItemRequest(dealVariant.Id, 2);

        // Act
        var response = await Client.PostAsJsonAsync("/api/cart/items", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CartResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] POST /api/cart/items: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        var cartInDb = await DbContext.CartItems
            .FirstOrDefaultAsync(c => c.UserId == customer.Id && c.FlashDealVariantId == dealVariant.Id);
        cartInDb.Should().NotBeNull();
        cartInDb!.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task AddItemToCart_WhenQuantityExceedsAvailableStock_ReturnsConflict_409()
    {
        // Arrange
        var (_, _, dealVariant) = await SetupCustomerAndFlashDealVariantAsync();
        var request = new AddCartItemRequest(dealVariant.Id, 999);

        // Act
        var response = await Client.PostAsJsonAsync("/api/cart/items", request);

        // Assert
        _output.WriteLine($"[TEST LOG] POST /api/cart/items (exceeding stock): {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetCartPage_WhenItemsExist_ReturnsGroupedShopItems()
    {
        // Arrange
        var (customer, shop, dealVariant) = await SetupCustomerAndFlashDealVariantAsync();
        var cartItem = new CartItem
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            FlashDealVariantId = dealVariant.Id,
            Quantity = 3,
            AddedAt = DateTime.UtcNow
        };
        DbContext.CartItems.Add(cartItem);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/cart/page");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CartPageResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] GET /api/cart/page: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Page.Items.Should().ContainSingle(s => s.ShopId == shop.Id);
    }

    [Fact]
    public async Task UpdateItemQuantity_ValidQuantity_ReturnsOkWithUpdatedQuantity()
    {
        // Arrange
        var (customer, _, dealVariant) = await SetupCustomerAndFlashDealVariantAsync();
        var cartItem = new CartItem
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            FlashDealVariantId = dealVariant.Id,
            Quantity = 1,
            AddedAt = DateTime.UtcNow
        };
        DbContext.CartItems.Add(cartItem);
        await DbContext.SaveChangesAsync();

        var updateRequest = new UpdateCartItemQuantityRequest(4);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/cart/items/{cartItem.Id}", updateRequest);

        // Assert
        _output.WriteLine($"[TEST LOG] PUT /api/cart/items/{cartItem.Id}: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var itemInDb = await DbContext.CartItems.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cartItem.Id);
        itemInDb.Should().NotBeNull();
        itemInDb!.Quantity.Should().Be(4);
    }

    [Fact]
    public async Task RemoveItem_WhenOwnedCartItem_ReturnsOkAndRemovesItem()
    {
        // Arrange
        var (customer, _, dealVariant) = await SetupCustomerAndFlashDealVariantAsync();
        var cartItem = new CartItem
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            FlashDealVariantId = dealVariant.Id,
            Quantity = 2,
            AddedAt = DateTime.UtcNow
        };
        DbContext.CartItems.Add(cartItem);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/cart/items/{cartItem.Id}");

        // Assert
        _output.WriteLine($"[TEST LOG] DELETE /api/cart/items/{cartItem.Id}: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var itemInDb = await DbContext.CartItems.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cartItem.Id);
        itemInDb.Should().BeNull();
    }

    [Fact]
    public async Task RemoveItem_WhenItemDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/cart/items/{nonExistentId}");

        // Assert
        _output.WriteLine($"[TEST LOG] DELETE /api/cart/items/{nonExistentId}: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CartEndpoints_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Act (No token attached)
        var response = await Client.GetAsync("/api/cart/summary");

        // Assert
        _output.WriteLine($"[TEST LOG] GET /api/cart/summary unauthenticated: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
