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

namespace SaveBite.IntegrationTests.Features.Products;

/// <summary>
/// Integration Tests for Admin Product Moderation:
/// - View Products for Admin (GET /api/products/admin)
/// - View Product Content (GET /api/products/admin/{productId})
/// - Remove Violating Product (DELETE /api/products/admin/{productId}/violation)
/// </summary>
public class AdminProductModerationIntegrationTests : BaseIntegrationTest
{
    public AdminProductModerationIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User owner, Shop shop, Category category, Product product)> SetupProductScenarioAsync()
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@savebite.com",
            fullName: "Shop Owner");

        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);

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
            Name = "Bánh Mì Thịt Nướng",
            Description = "Delicious crispy bread",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        return (owner, shop, category, product);
    }

    // =========================================================================
    // 1. View Products for Admin (GET /api/products/admin)
    // =========================================================================

    [Fact]
    public async Task ViewProductsForAdmin_WhenAdmin_Returns200_WithPagedList()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync($"admin_{Guid.NewGuid():N}@savebite.com");
        var (_, _, _, product) = await SetupProductScenarioAsync();
        SetBearerToken(admin.Id, admin.Email, admin.Role);

        // Act
        var response = await Client.GetAsync("/api/products/admin?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductListResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().Contain(p => p.Id == product.Id);
    }

    [Fact]
    public async Task ViewProductsForAdmin_WhenCustomer_Returns403Forbidden()
    {
        // Arrange
        await AuthenticateAsCustomerAsync($"cust_{Guid.NewGuid():N}@savebite.com");

        // Act
        var response = await Client.GetAsync("/api/products/admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =========================================================================
    // 2. View Product Content (GET /api/products/admin/{productId})
    // =========================================================================

    [Fact]
    public async Task ViewProductContentForAdmin_WhenValidProduct_Returns200_WithDetails()
    {
        // Arrange
        var (_, _, _, product) = await SetupProductScenarioAsync();
        await AuthenticateAsAdminAsync($"admin_{Guid.NewGuid():N}@savebite.com");

        // Act
        var response = await Client.GetAsync($"/api/products/admin/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductContentResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(product.Id);
        body.Data.Name.Should().Be("Bánh Mì Thịt Nướng");
    }

    [Fact]
    public async Task ViewProductContentForAdmin_WhenNotFound_Returns404()
    {
        // Arrange
        await AuthenticateAsAdminAsync($"admin_{Guid.NewGuid():N}@savebite.com");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/products/admin/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // 3. Remove Violating Product (DELETE /api/products/admin/{productId}/violation)
    // =========================================================================

    [Fact]
    public async Task RemoveViolatingProduct_WhenAdmin_SoftDeletesAndReturns200()
    {
        // Arrange
        var (_, _, _, product) = await SetupProductScenarioAsync();
        await AuthenticateAsAdminAsync($"admin_{Guid.NewGuid():N}@savebite.com");

        var request = new RemoveViolatingProductRequest
        {
            Reason = "Product contains prohibited or expired ingredients"
        };

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/admin/{product.Id}/violation")
        {
            Content = JsonContent.Create(request)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ModerationActionResponse>>();
        body.Should().NotBeNull();
        body!.Data!.Action.Should().Be("PRODUCT_REMOVED_VIOLATION");

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var productInDb = await DbContext.Products.FindAsync(product.Id);
        productInDb.Should().NotBeNull();
        productInDb!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveViolatingProduct_WhenCustomer_Returns403Forbidden()
    {
        // Arrange
        var (_, _, _, product) = await SetupProductScenarioAsync();
        await AuthenticateAsCustomerAsync($"cust_{Guid.NewGuid():N}@savebite.com");

        var request = new RemoveViolatingProductRequest
        {
            Reason = "Attempted customer removal"
        };

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/products/admin/{product.Id}/violation")
        {
            Content = JsonContent.Create(request)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
