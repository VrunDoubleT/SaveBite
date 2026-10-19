using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.CategoryManagement;

/// <summary>
/// Unit Tests for Feature: View Category List
/// Service: CategoryService.GetCategoriesAsync
/// Function Code: UT-38
/// Sheet Name: UT_ViewCategoryList
/// </summary>
public class ViewCategoryListTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ILogger<CategoryService>> _loggerMock = new();
    private readonly CategoryService _service;

    public ViewCategoryListTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new CategoryService(
            _categoryRepoMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private List<Category> CreateSampleCategories()
    {
        return new List<Category>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Bakery & Pastry",
                Description = "Fresh breads, croissants and cakes",
                ImageUrl = "https://res.cloudinary.com/savebite/bakery.jpg",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Beverages",
                Description = "Coffee, tea, and fruit juices",
                ImageUrl = "https://res.cloudinary.com/savebite/beverages.jpg",
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };
    }

    // =========================================================================
    // TCID01: Danh sách danh mục có dữ liệu trong DB (categories Has items) -> Trả về danh sách (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetCategories_HasItems_ReturnsCategoryList_Normal()
    {
        // Arrange
        var request = new GetCategoriesRequest();
        var categories = CreateSampleCategories();
        var pagedResult = PagedResult<Category>.Create(categories, 1, 10, categories.Count);

        _categoryRepoMock
            .Setup(r => r.GetPagedCategoriesAsync(It.IsAny<GetCategoriesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetCategoriesAsync(request);

        // Assert & Log
        PrintSuccess("TCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Name.Should().Be("Bakery & Pastry");
        result.Items.First().IsActive.Should().BeTrue();

        _categoryRepoMock.Verify(r => r.GetPagedCategoriesAsync(It.IsAny<GetCategoriesRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Danh sách danh mục không có dữ liệu (categories Empty) -> Trả về danh sách rỗng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetCategories_Empty_ReturnsEmptyList_Normal()
    {
        // Arrange
        var request = new GetCategoriesRequest();
        var pagedResult = PagedResult<Category>.Create(new List<Category>(), 1, 10, 0);

        _categoryRepoMock
            .Setup(r => r.GetPagedCategoriesAsync(It.IsAny<GetCategoriesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetCategoriesAsync(request);

        // Assert & Log
        PrintSuccess("TCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        result.Items.Should().BeEmpty();

        _categoryRepoMock.Verify(r => r.GetPagedCategoriesAsync(It.IsAny<GetCategoriesRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================
    // Helper Methods for Formatting Log Output
    // =========================================================
    private void PrintSuccess<T>(string tcid, T result, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }
}
