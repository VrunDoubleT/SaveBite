using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.CategoryManagement;

/// <summary>
/// Unit Tests for Feature: Delete Category
/// Service: CategoryService.DeleteCategoryAsync
/// Function Code: UT-41
/// Sheet Name: UT_DeleteCategory
/// </summary>
public class DeleteCategoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ILogger<CategoryService>> _loggerMock = new();
    private readonly CategoryService _service;

    private readonly Guid _existingCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _nonExistentCategoryId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public DeleteCategoryTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new CategoryService(
            _categoryRepoMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private Category CreateSampleCategory() => new()
    {
        Id = _existingCategoryId,
        Name = "Bakery & Pastry",
        Description = "Fresh artisan breads and cakes",
        ImageUrl = "https://res.cloudinary.com/savebite/bakery.png",
        IsActive = true,
        CreatedAt = DateTime.UtcNow.AddMonths(-1),
        UpdatedAt = DateTime.UtcNow.AddDays(-3)
    };

    // =========================================================================
    // TCID01: Xóa danh mục thành công khi tồn tại và không có sản phẩm liên kết (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_DeleteCategory_ExistsNoLinkedProducts_DeletesSuccessfully_Normal()
    {
        // Arrange
        var category = CreateSampleCategory();

        _categoryRepoMock
            .Setup(r => r.GetCategoryByIdAsync(_existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepoMock
            .Setup(r => r.HasLinkedProductsAsync(_existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _categoryRepoMock
            .Setup(r => r.DeleteCategory(It.IsAny<Category>()));

        _categoryRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteCategoryAsync(_existingCategoryId);

        // Assert & Log
        PrintSuccess("TCID01", "Category deleted successfully");

        _categoryRepoMock.Verify(r => r.HasLinkedProductsAsync(_existingCategoryId, It.IsAny<CancellationToken>()), Times.Once);
        _categoryRepoMock.Verify(r => r.DeleteCategory(It.Is<Category>(c => c.Id == _existingCategoryId)), Times.Once);
        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Không tìm thấy danh mục trong DB -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_DeleteCategory_CategoryNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _categoryRepoMock
            .Setup(r => r.GetCategoryByIdAsync(_nonExistentCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act
        var act = () => _service.DeleteCategoryAsync(_nonExistentCategoryId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Category not found.");

        _categoryRepoMock.Verify(r => r.HasLinkedProductsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _categoryRepoMock.Verify(r => r.DeleteCategory(It.IsAny<Category>()), Times.Never);
        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID03: Danh mục đang liên kết với sản phẩm -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_DeleteCategory_HasLinkedProducts_ThrowsConflict_Abnormal()
    {
        // Arrange
        var category = CreateSampleCategory();

        _categoryRepoMock
            .Setup(r => r.GetCategoryByIdAsync(_existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepoMock
            .Setup(r => r.HasLinkedProductsAsync(_existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.DeleteCategoryAsync(_existingCategoryId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Cannot delete this category because it is currently linked to active products. Please update it to 'Inactive' instead.");

        _categoryRepoMock.Verify(r => r.DeleteCategory(It.IsAny<Category>()), Times.Never);
        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================
    // Helper Methods for Formatting Log Output
    // =========================================================
    private void PrintSuccess(string tcid, string message, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }

    private void PrintException(string tcid, AppException ex)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"ERROR CODE: {ex.ErrorCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }
}
