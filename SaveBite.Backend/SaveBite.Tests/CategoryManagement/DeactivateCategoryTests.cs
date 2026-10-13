using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.CategoryManagement;

/// <summary>
/// Unit Tests for Feature: Deactivate Category
/// Service: CategoryService.UpdateCategoryStatusAsync (IsActive = false)
/// Function Code: UT-43
/// Sheet Name: UT_DeactivateCategory
/// </summary>
public class DeactivateCategoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ILogger<CategoryService>> _loggerMock = new();
    private readonly CategoryService _service;

    private readonly Guid _existingCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _nonExistentCategoryId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public DeactivateCategoryTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new CategoryService(
            _categoryRepoMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private Category CreateSampleCategory(bool isActive) => new()
    {
        Id = _existingCategoryId,
        Name = "Bakery & Pastry",
        Description = "Fresh artisan breads and cakes",
        ImageUrl = "https://res.cloudinary.com/savebite/bakery.png",
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow.AddMonths(-1),
        UpdatedAt = DateTime.UtcNow.AddDays(-3)
    };

    // =========================================================================
    // TCID01: Vô hiệu hóa danh mục thành công khi đang Active -> IsActive = false (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_DeactivateCategory_CurrentlyActive_DeactivatesSuccessfully_Normal()
    {
        // Arrange
        var category = CreateSampleCategory(isActive: true);
        var request = new UpdateCategoryStatusRequest { IsActive = false };

        _categoryRepoMock
            .Setup(r => r.GetCategoryByIdAsync(_existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateCategoryStatusAsync(_existingCategoryId, request);

        // Assert & Log
        PrintSuccess("TCID01", "Category deactivated successfully");
        category.IsActive.Should().BeFalse();
        category.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Không tìm thấy danh mục -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_DeactivateCategory_CategoryNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateCategoryStatusRequest { IsActive = false };

        _categoryRepoMock
            .Setup(r => r.GetCategoryByIdAsync(_nonExistentCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act
        var act = () => _service.UpdateCategoryStatusAsync(_nonExistentCategoryId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Category not found.");

        _categoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID03: Danh mục đã Inactive sẵn -> Bỏ qua, không cập nhật DB (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID03_DeactivateCategory_AlreadyInactive_SkipsUpdate_Normal()
    {
        // Arrange
        var category = CreateSampleCategory(isActive: false);
        var request = new UpdateCategoryStatusRequest { IsActive = false };

        _categoryRepoMock
            .Setup(r => r.GetCategoryByIdAsync(_existingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Act
        await _service.UpdateCategoryStatusAsync(_existingCategoryId, request);

        // Assert & Log
        PrintSuccess("TCID03", "Already inactive - skipped update (no SaveChanges called)");
        category.IsActive.Should().BeFalse();

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
