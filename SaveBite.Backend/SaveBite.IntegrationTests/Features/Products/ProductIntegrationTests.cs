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

public class ProductIntegrationTests : BaseIntegrationTest
{
    public ProductIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User owner, Shop shop, Category category)> SetupStoreOwnerWithShopAndCategoryAsync()
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@test.com",
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
        await DbContext.SaveChangesAsync();

        return (owner, shop, category);
    }

    [Fact]
    public async Task GetProducts_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Act (Không gửi token)
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_WhenCustomerCalls_ReturnsForbidden_403()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act (Customer không có quyền StoreOwnerAccess)
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProducts_WhenStoreOwnerCalls_ReturnsPagedResult_200()
    {
        // Arrange
        var (_, shop, category) = await SetupStoreOwnerWithShopAndCategoryAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Bánh Su Kem",
            Description = "Bánh kem vani thơm ngọt",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductListItemResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().Contain(p => p.Id == product.Id && p.Name == "Bánh Su Kem");
    }

    [Fact]
    public async Task GetSummary_WhenStoreOwnerCalls_ReturnsSummary_200()
    {
        // Arrange
        var (_, shop, category) = await SetupStoreOwnerWithShopAndCategoryAsync();

        var activeProduct = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Active Bread",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var hiddenProduct = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Hidden Bread",
            Status = ProductStatus.Hidden,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Products.AddRange(activeProduct, hiddenProduct);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/products/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductSummaryResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TotalProducts.Should().Be(2);
        body.Data.ActiveProducts.Should().Be(1);
        body.Data.HiddenProducts.Should().Be(1);
    }

    [Fact]
    public async Task CreateProduct_WhenValidMultipartFormData_CreatesProductAndReturns_201()
    {
        // Arrange
        var (_, _, category) = await SetupStoreOwnerWithShopAndCategoryAsync();

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(category.Id.ToString()), "CategoryId");
        form.Add(new StringContent("Bánh Mì Thịt Nguội"), "Name");
        form.Add(new StringContent("Bánh mì truyền thống đặc biệt"), "Description");
        form.Add(new StringContent("35000"), "Variants[0].OriginalPrice");
        form.Add(new StringContent("20000"), "Variants[0].DealPrice");

        // Act
        var response = await Client.PostAsync("/api/products", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Name.Should().Be("Bánh Mì Thịt Nguội");

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var productInDb = await DbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == body.Data.Id);
        productInDb.Should().NotBeNull();
        productInDb!.Name.Should().Be("Bánh Mì Thịt Nguội");
    }

    [Fact]
    public async Task GetProductById_WhenExists_ReturnsProductDetails_200()
    {
        // Arrange
        var (_, shop, category) = await SetupStoreOwnerWithShopAndCategoryAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Bánh Sừng Bò Croissant",
            Description = "Bơ Pháp béo ngậy",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(product.Id);
        body.Data.Name.Should().Be("Bánh Sừng Bò Croissant");
    }

    [Fact]
    public async Task GetProductById_WhenNotFound_ReturnsNotFound_404()
    {
        // Arrange
        await SetupStoreOwnerWithShopAndCategoryAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_WhenValid_UpdatesDatabaseAndReturns_200()
    {
        // Arrange
        var (_, shop, category) = await SetupStoreOwnerWithShopAndCategoryAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Tên Cũ",
            Description = "Mô tả cũ",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var updateRequest = new UpdateProductRequest
        {
            CategoryId = category.Id,
            Name = "Tên Đã Cập Nhật",
            Description = "Mô tả đã cập nhật"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/products/{product.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var updatedInDb = await DbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.Name.Should().Be("Tên Đã Cập Nhật");
        updatedInDb.Description.Should().Be("Mô tả đã cập nhật");
    }

    [Fact]
    public async Task UpdateStatus_WhenHidden_UpdatesDatabaseAndReturns_200()
    {
        // Arrange
        var (_, shop, category) = await SetupStoreOwnerWithShopAndCategoryAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Sản phẩm thử nghiệm",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        var statusRequest = new UpdateProductStatusRequest
        {
            Status = ProductStatus.Hidden
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/products/{product.Id}/status", statusRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var updatedInDb = await DbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.Status.Should().Be(ProductStatus.Hidden);
    }

    [Fact]
    public async Task DeleteProduct_WhenExists_SoftDeletesInDatabaseAndReturns_200()
    {
        // Arrange
        var (_, shop, category) = await SetupStoreOwnerWithShopAndCategoryAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Sản phẩm sắp xóa",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/products/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK); 

        // Verify Database: soft delete nghĩa là DeletedAt có giá trị và Status chuyển sang Hidden
        DbContext.ChangeTracker.Clear();
        var deletedInDb = await DbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
        deletedInDb.Should().NotBeNull();
        deletedInDb!.DeletedAt.Should().NotBeNull();
        deletedInDb.Status.Should().Be(ProductStatus.Hidden);
    }
}
