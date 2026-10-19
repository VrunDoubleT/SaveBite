using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.IntegrationTests.Features.Categories;

public class CategoryIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public CategoryIntegrationTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task GetCatalog_ReturnsOk_WithActiveCategoriesOnly()
    {
        // 1. Arrange: Tạo 1 danh mục Active và 1 danh mục Ẩn vào DB test
        var activeCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Bánh Mì Test",
            Description = "Bánh mì các loại",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Đồ Ăn Đã Ẩn Test",
            Description = "Category ẩn",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Categories.AddRange(activeCategory, inactiveCategory);
        await DbContext.SaveChangesAsync();

        // 2. Act: Bắn request GET public
        var response = await Client.GetAsync("/api/categories/catalog");
        var responseBody = await response.Content.ReadAsStringAsync();

        // IN LOG RA RIDER OUTPUT:
        _output.WriteLine($"[TEST LOG] URL: GET /api/categories/catalog");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Kiểm tra Status Code = 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().Contain(c => c.Name == "Bánh Mì Test");
        result.Data.Should().NotContain(c => c.Name == "Đồ Ăn Đã Ẩn Test");
    }

    [Fact]
    public async Task CreateCategory_WithoutToken_ReturnsUnauthorized()
    {
        // 1. Arrange
        var request = new CreateCategoryRequest
        {
            Name = "Món Nhật Mới",
            Description = "Sushi và Sashimi",
            IsActive = true
        };

        // 2. Act: Gọi API Admin mà KHÔNG truyền Bearer Token
        var response = await Client.PostAsJsonAsync("/api/categories", request);
        var responseBody = await response.Content.ReadAsStringAsync();

        // IN LOG RA RIDER OUTPUT:
        _output.WriteLine($"[TEST LOG] URL: POST /api/categories (No Token)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Phải bị chặn 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_AsAdmin_ReturnsOk_AndSavesToDatabase()
    {
        // 1. Arrange: Đăng nhập với quyền Admin
        await AuthenticateAsAdminAsync();

        var request = new CreateCategoryRequest
        {
            Name = "Đồ Uống Giải Khát",
            Description = "Trà sữa, nước ép trái cây",
            IsActive = true
        };

        // 2. Act: Gửi request tạo danh mục
        var response = await Client.PostAsJsonAsync("/api/categories", request);
        var responseBody = await response.Content.ReadAsStringAsync();

        // IN LOG RA RIDER OUTPUT:
        _output.WriteLine($"[TEST LOG] URL: POST /api/categories (As Admin)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: HTTP status = 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Assert: Kiểm tra PostgreSQL thật xem đã được lưu xuống chưa
        var savedCategory = await DbContext.Categories
            .FirstOrDefaultAsync(c => c.Name == "Đồ Uống Giải Khát");

        savedCategory.Should().NotBeNull();
        savedCategory!.Description.Should().Be("Trà sữa, nước ép trái cây");
        savedCategory.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_ReturnsConflict_409()
    {
        // 1. Arrange: Đăng nhập Admin và tạo sẵn 1 Category có tên "Cơm Tấm" trong DB
        await AuthenticateAsAdminAsync();

        DbContext.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            Name = "Cơm Tấm",
            Description = "Cơm sườn bì chả",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await DbContext.SaveChangesAsync();

        var duplicateRequest = new CreateCategoryRequest
        {
            Name = "Cơm Tấm", // Trùng tên
            Description = "Cơm tấm đêm",
            IsActive = true
        };

        // 2. Act: Thử tạo trùng tên
        var response = await Client.PostAsJsonAsync("/api/categories", duplicateRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        // IN LOG RA RIDER OUTPUT:
        _output.WriteLine($"[TEST LOG] URL: POST /api/categories (Duplicate Name)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Phải trả về 409 Conflict
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetCategoryById_WhenNotFound_ReturnsNotFound_404()
    {
        // 1. Arrange: Đăng nhập Admin và tạo 1 ID ngẫu nhiên không có trong DB
        await AuthenticateAsAdminAsync();
        var nonExistentId = Guid.NewGuid();

        // 2. Act: Gọi GET /api/categories/{id}
        var response = await Client.GetAsync($"/api/categories/{nonExistentId}");
        var responseBody = await response.Content.ReadAsStringAsync();

        // IN LOG RA RIDER OUTPUT:
        _output.WriteLine($"[TEST LOG] URL: GET /api/categories/{nonExistentId} (Not Found)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Phải trả về 404 Not Found
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_WhenValid_ReturnsOk_AndUpdatesDatabase()
    {
        // 1. Arrange: Đăng nhập Admin và tạo 1 Category trong DB
        await AuthenticateAsAdminAsync();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Lẩu Nướng Cũ",
            Description = "Mô tả cũ",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        var updateRequest = new UpdateCategoryRequest
        {
            Name = "Lẩu Nướng BBQ Mới",
            Description = "Mô tả đã được cập nhật"
        };

        // 2. Act: Gửi PUT /api/categories/{id}
        var response = await Client.PutAsJsonAsync($"/api/categories/{category.Id}", updateRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        // IN LOG RA RIDER OUTPUT:
        _output.WriteLine($"[TEST LOG] URL: PUT /api/categories/{category.Id}");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Status = 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Assert: Xóa cache EF Core và kiểm tra Database thật xem tên và mô tả đã đổi chưa
        DbContext.ChangeTracker.Clear();
        var updatedCategory = await DbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == category.Id);

        updatedCategory.Should().NotBeNull();
        updatedCategory!.Name.Should().Be("Lẩu Nướng BBQ Mới");
        updatedCategory.Description.Should().Be("Mô tả đã được cập nhật");
    }

    [Fact]
    public async Task DeleteCategory_WhenNoLinkedProducts_ReturnsOk_AndRemovesFromDatabase()
    {
        // 1. Arrange: Đăng nhập Admin và tạo 1 Category trong DB
        await AuthenticateAsAdminAsync();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Danh Mục Muốn Xóa",
            Description = "Sắp bị xóa",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();

        // 2. Act: Gửi DELETE /api/categories/{id}
        var response = await Client.DeleteAsync($"/api/categories/{category.Id}");
        var responseBody = await response.Content.ReadAsStringAsync();

        // IN LOG RA RIDER OUTPUT:
        _output.WriteLine($"[TEST LOG] URL: DELETE /api/categories/{category.Id}");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Status = 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Assert: Xóa cache EF Core và kiểm tra PostgreSQL thật xem dòng này đã biến mất hoàn toàn chưa
        DbContext.ChangeTracker.Clear();
        var deletedCategory = await DbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == category.Id);

        deletedCategory.Should().BeNull();
    }
}
