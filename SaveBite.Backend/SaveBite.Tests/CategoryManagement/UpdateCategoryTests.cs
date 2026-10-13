using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.CategoryManagement;

/// <summary>
/// Unit Tests for Feature: Update Category
/// Service: CategoryService.UpdateCategoryAsync & UpdateCategoryRequestValidator
/// Function Code: UT-40
/// Sheet Name: UT_UpdateCategory
/// </summary>
public class UpdateCategoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ILogger<CategoryService>> _loggerMock = new();
    private readonly CategoryService _service;
    private readonly UpdateCategoryRequestValidator _validator = new();

    private readonly Guid _existingCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _nonExistentCategoryId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public UpdateCategoryTests(ITestOutputHelper output)
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
    // TCID01: Cập nhật danh mục thành công với tên hợp lệ và chưa trùng với danh mục khác (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_UpdateCategory_ValidName_NotDuplicated_UpdatesSuccessfully_Normal()
    {
        // Arrange
        var category = CreateSampleCategory();
        var request = new UpdateCategoryRequest
        {
            Name = "Fresh Bakery",
            Description = "Updated description",
            ImageUrl = "https://res.cloudinary.com/savebite/fresh-bakery.png"
        };

        _categoryRepoMock
            .Setup(r => r.GetCategoryByIdAsync(_existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepoMock
            .Setup(r => r.CategoryNameExistsAsync("Fresh Bakery", _existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _categoryRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateCategoryAsync(_existingCategoryId, request);

        // Assert & Log
        PrintSuccess("TCID01", "Category updated successfully");
        category.Name.Should().Be("Fresh Bakery");
        category.Description.Should().Be("Updated description");
        category.ImageUrl.Should().Be("https://res.cloudinary.com/savebite/fresh-bakery.png");
        category.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Không tìm thấy danh mục trong DB -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_UpdateCategory_CategoryNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateCategoryRequest
        {
            Name = "Fresh Bakery",
            Description = "Some description",
            ImageUrl = null
        };

        _categoryRepoMock
            .Setup(r => r.GetCategoryByIdAsync(_nonExistentCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act
        var act = () => _service.UpdateCategoryAsync(_nonExistentCategoryId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Category not found.");

        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID03: Tên mới đã tồn tại ở danh mục khác -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_UpdateCategory_NameExistsInAnotherCategory_ThrowsConflict_Abnormal()
    {
        // Arrange
        var category = CreateSampleCategory();
        var request = new UpdateCategoryRequest
        {
            Name = "Beverages",
            Description = "Updated description",
            ImageUrl = null
        };

        _categoryRepoMock
            .Setup(r => r.GetCategoryByIdAsync(_existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepoMock
            .Setup(r => r.CategoryNameExistsAsync("Beverages", _existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.UpdateCategoryAsync(_existingCategoryId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Another category with this name already exists.");

        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID04: Tên danh mục để trống -> Validator từ chối (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID04_UpdateCategory_EmptyName_ValidationFails_Boundary()
    {
        // Arrange
        var request = new UpdateCategoryRequest
        {
            Name = "",
            Description = "Some description",
            ImageUrl = null
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("TCID04", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("The category name is required."));
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

    private void PrintValidationError(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 BadRequest (Validation Failed)");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }
}
