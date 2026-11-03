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

namespace SaveBite.IntegrationTests.Features.Warnings;

/// <summary>
/// Integration Tests for Warning Management:
/// - Send Violation Warning (POST /api/warnings/admin/products/{productId})
/// - View Warning History (GET /api/warnings)
/// - View Warning Details (GET /api/warnings/{warningId})
/// </summary>
public class WarningModerationIntegrationTests : BaseIntegrationTest
{
    public WarningModerationIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User owner, Shop shop, Category category, Product product, ProductContentRevision revision)> SetupProductWithRevisionAsync()
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
            Name = "Bánh Mì Pate",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Products.Add(product);

        var revision = new ProductContentRevision
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            RevisionNumber = 1,
            SubmittedByUserId = owner.Id,
            CategoryId = category.Id,
            Name = "Bánh Mì Pate",
            Status = ProductReviewStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };
        DbContext.ProductContentRevisions.Add(revision);
        await DbContext.SaveChangesAsync();

        return (owner, shop, category, product, revision);
    }

    // =========================================================================
    // 1. Send Violation Warning (POST /api/warnings/admin/products/{productId})
    // =========================================================================

    [Fact]
    public async Task IssueWarning_WhenAdmin_CreatesWarning_Returns200()
    {
        // Arrange
        var (_, _, _, product, _) = await SetupProductWithRevisionAsync();
        await AuthenticateAsAdminAsync($"admin_{Guid.NewGuid():N}@savebite.com");

        var request = new IssueViolationWarningRequest
        {
            Reason = "Misleading product ingredient declaration",
            RequiredCorrection = "Update ingredient description to indicate presence of allergen peanuts"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/warnings/admin/products/{product.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ModerationActionResponse>>();
        body.Should().NotBeNull();
        body!.Data!.Action.Should().Be("SHOP_WARNING_ISSUED");

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var warningInDb = await DbContext.ProductWarnings
            .FirstOrDefaultAsync(w => w.ProductId == product.Id);
        warningInDb.Should().NotBeNull();
        warningInDb!.Reason.Should().Be(request.Reason);
    }

    [Fact]
    public async Task IssueWarning_WhenCustomer_Returns403Forbidden()
    {
        // Arrange
        var (_, _, _, product, _) = await SetupProductWithRevisionAsync();
        await AuthenticateAsCustomerAsync($"cust_{Guid.NewGuid():N}@savebite.com");

        var request = new IssueViolationWarningRequest
        {
            Reason = "Customer attempt",
            RequiredCorrection = "Correction text"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/warnings/admin/products/{product.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // =========================================================================
    // 2. View Warning History (GET /api/warnings)
    // =========================================================================

    [Fact]
    public async Task ViewWarningHistory_WhenStoreOwner_Returns200_WithPagedWarnings()
    {
        // Arrange
        var (owner, shop, _, product, revision) = await SetupProductWithRevisionAsync();
        var warning = new ProductWarning
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ShopId = shop.Id,
            IssuedByUserId = owner.Id,
            BaselineRevisionId = revision.Id,
            ProductName = product.Name,
            Reason = "Inaccurate price listing",
            RequiredCorrection = "Correct base price",
            DueAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        DbContext.ProductWarnings.Add(warning);
        await DbContext.SaveChangesAsync();

        // Switch to Store Owner token
        SetBearerToken(owner.Id, owner.Email, owner.Role);

        // Act
        var response = await Client.GetAsync("/api/warnings?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<WarningResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().Contain(w => w.Id == warning.Id);
    }

    // =========================================================================
    // 3. View Warning Details (GET /api/warnings/{warningId})
    // =========================================================================

    [Fact]
    public async Task ViewWarningDetails_WhenStoreOwnerOwnsShop_Returns200_WithWarningDetails()
    {
        // Arrange
        var (owner, shop, _, product, revision) = await SetupProductWithRevisionAsync();
        var warning = new ProductWarning
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            ShopId = shop.Id,
            IssuedByUserId = owner.Id,
            BaselineRevisionId = revision.Id,
            ProductName = product.Name,
            Reason = "Missing allergen note",
            RequiredCorrection = "Add allergen note",
            DueAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        DbContext.ProductWarnings.Add(warning);
        await DbContext.SaveChangesAsync();

        // Switch to Store Owner token
        SetBearerToken(owner.Id, owner.Email, owner.Role);

        // Act
        var response = await Client.GetAsync($"/api/warnings/{warning.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<WarningResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(warning.Id);
        body.Data.Reason.Should().Be(warning.Reason);
    }
}
