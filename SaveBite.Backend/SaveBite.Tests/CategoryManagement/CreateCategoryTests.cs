using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
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
/// Unit Tests for Feature: Create Category
/// Service: CategoryService.CreateCategoryAsync & CreateCategoryRequestValidator
/// Function Code: UT-39
/// Sheet Name: UT_CreateCategory
/// </summary>
public class CreateCategoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ILogger<CategoryService>> _loggerMock = new();
    private readonly CategoryService _service;
    private readonly CreateCategoryRequestValidator _validator = new();

    public CreateCategoryTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new CategoryService(
            _categoryRepoMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    // =========================================================================
    // TCID01: Tạo danh mục mới thành công với tên hợp lệ và chưa tồn tại trong DB (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_CreateCategory_ValidNameNotExists_CreatesSuccessfully_Normal()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "Fresh Bakery & Pastry",
            Description = "Fresh artisan breads and cakes",
            ImageUrl = "https://res.cloudinary.com/savebite/bakery.png",
            IsActive = true
        };

        _categoryRepoMock
            .Setup(r => r.CategoryNameExistsAsync("Fresh Bakery & Pastry", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _categoryRepoMock
            .Setup(r => r.AddCategory(It.IsAny<Category>()));

        _categoryRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var categoryId = await _service.CreateCategoryAsync(request);

        // Assert & Log
        PrintSuccess("TCID01", new { CategoryId = categoryId, Name = request.Name });
        categoryId.Should().NotBeEmpty();

        _categoryRepoMock.Verify(r => r.CategoryNameExistsAsync("Fresh Bakery & Pastry", null, It.IsAny<CancellationToken>()), Times.Once);
        _categoryRepoMock.Verify(r => r.AddCategory(It.Is<Category>(c =>
            c.Name == "Fresh Bakery & Pastry" &&
            c.Description == "Fresh artisan breads and cakes" &&
            c.ImageUrl == "https://res.cloudinary.com/savebite/bakery.png" &&
            c.IsActive == true
        )), Times.Once);
        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Tên danh mục đã tồn tại trong DB -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_CreateCategory_NameAlreadyExists_ThrowsConflict_Abnormal()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "Beverages",
            Description = "Coffee and tea drinks",
            ImageUrl = null,
            IsActive = true
        };

        _categoryRepoMock
            .Setup(r => r.CategoryNameExistsAsync("Beverages", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateCategoryAsync(request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("A category with this name already exists.");

        _categoryRepoMock.Verify(r => r.CategoryNameExistsAsync("Beverages", null, It.IsAny<CancellationToken>()), Times.Once);
        _categoryRepoMock.Verify(r => r.AddCategory(It.IsAny<Category>()), Times.Never);
        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID03: Tên danh mục để trống -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_CreateCategory_EmptyName_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = "",
            Description = "Delicious desserts",
            ImageUrl = null,
            IsActive = true
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("TCID03", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("The category name is required."));
    }

    // =========================================================================
    // TCID04: Tên danh mục vượt quá 150 ký tự -> Validator từ chối (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID04_CreateCategory_NameExceeds150Chars_ValidationFails_Boundary()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            Name = new string('A', 151),
            Description = "Long category name boundary test",
            ImageUrl = null,
            IsActive = true
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("TCID04", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("The category name must not exceed 150 characters."));
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
