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

namespace SaveBite.IntegrationTests.Features.Shops;

/// <summary>
/// Integration Tests for Admin Shop Moderation:
/// - View Shops for Admin (GET /api/shops/admin)
/// - View Shop Content (GET /api/shops/admin/{shopId})
/// - Update Shop Status (PATCH /api/shops/admin/{shopId}/status)
/// </summary>
public class AdminShopModerationIntegrationTests : BaseIntegrationTest
{
    public AdminShopModerationIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User owner, Shop shop)> SetupShopScenarioAsync()
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@savebite.com",
            fullName: "Shop Owner");

        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);
        return (owner, shop);
    }

    // =========================================================================
    // 1. View Shops for Admin (GET /api/shops/admin)
    // =========================================================================

    [Fact]
    public async Task ViewShopsForAdmin_WhenAdmin_Returns200_WithPagedList()
    {
        // Arrange
        var (_, shop) = await SetupShopScenarioAsync();
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/shops/admin?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ShopListResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().Contain(s => s.Id == shop.Id);
    }

    [Fact]
    public async Task ViewShopsForAdmin_WhenCustomer_Returns403Forbidden()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/shops/admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =========================================================================
    // 2. View Shop Content (GET /api/shops/admin/{shopId})
    // =========================================================================

    [Fact]
    public async Task ViewShopContentForAdmin_WhenValidShop_Returns200_WithDetails()
    {
        // Arrange
        var (_, shop) = await SetupShopScenarioAsync();
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync($"/api/shops/admin/{shop.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ShopContentResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(shop.Id);
        body.Data.Name.Should().Be(shop.Name);
    }

    [Fact]
    public async Task ViewShopContentForAdmin_WhenNotFound_Returns404()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/shops/admin/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // 3. Update Shop Status (PATCH /api/shops/admin/{shopId}/status)
    // =========================================================================

    [Fact]
    public async Task UpdateShopStatus_ActiveToSuspended_Returns200_AndUpdatesDatabase()
    {
        // Arrange
        var (_, shop) = await SetupShopScenarioAsync();
        await AuthenticateAsAdminAsync();

        var request = new UpdateShopModerationStatusRequest
        {
            Status = "Suspended",
            Reason = "Repeated hygiene standard violations reported"
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/shops/admin/{shop.Id}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ModerationActionResponse>>();
        body.Should().NotBeNull();
        body!.Data!.Action.Should().Be("SHOP_STATUS_UPDATED");

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var shopInDb = await DbContext.Shops.FindAsync(shop.Id);
        shopInDb.Should().NotBeNull();
        shopInDb!.Status.Should().Be(ShopStatus.Suspended);
    }

    [Fact]
    public async Task UpdateShopStatus_WhenCustomer_Returns403Forbidden()
    {
        // Arrange
        var (_, shop) = await SetupShopScenarioAsync();
        await AuthenticateAsCustomerAsync();

        var request = new UpdateShopModerationStatusRequest
        {
            Status = "Suspended",
            Reason = "Customer attempt"
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/shops/admin/{shop.Id}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
