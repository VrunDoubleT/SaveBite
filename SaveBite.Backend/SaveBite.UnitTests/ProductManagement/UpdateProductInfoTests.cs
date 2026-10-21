using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.ProductManagement;

/// <summary>
/// Unit Tests for Feature: Update Product Info
/// Service: ProductService.UpdateAsync & UpdateProductRequestValidator
/// Function Code: UT_UpdateProductInfo
/// Sheet Name: UT_UpdateProductInfo
/// Business Rules: BR-073, BR-074
/// </summary>
public class UpdateProductInfoTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly ProductService _service;
    private readonly UpdateProductRequestValidator _validator = new();

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _originalCategoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _newCategoryId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly Guid _inactiveCategoryId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private readonly Guid _nonExistentProductId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public UpdateProductInfoTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ProductService(
            _repositoryMock.Object,
            _cloudinaryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    // =========================================================================
    // UTCID01: Store owner updates product info with active category successfully (Normal - N)
    // Business Rules: BR-073, BR-074
    // =========================================================================
    [Fact]
    public async Task UTCID01_UpdateProduct_ValidData_UpdatesSuccessfully_Normal()
    {
        // Arrange
        var request = new UpdateProductRequest
        {
            CategoryId = _newCategoryId,
            Name = "Updated Artisan Bread",
            Description = "Updated fresh artisan bread description"
        };

        var product = new Product
        {
            Id = _productId,
            ShopId = _shopId,
            CategoryId = _originalCategoryId,
            Name = "Original Bread",
            Description = "Original description",
            Status = ProductStatus.Active,
            Category = new Category { Id = _originalCategoryId, Name = "Original Category" },
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
            Variants = new List<ProductVariant>
            {
                new() { Id = Guid.NewGuid(), ProductId = _productId, OriginalPrice = 30000m, DealPrice = 20000m, IsActive = true }
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.ActiveCategoryExistsAsync(_newCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(_userId, _productId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        product.Name.Should().Be("Updated Artisan Bread");
        product.Description.Should().Be("Updated fresh artisan bread description");
        product.CategoryId.Should().Be(_newCategoryId);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: User has no shop or shop is unavailable -> Throws NotFound (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID02_UpdateProduct_NoShopAvailable_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateProductRequest
        {
            CategoryId = _newCategoryId,
            Name = "Updated Bread"
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductShopOption?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(_userId, _productId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Shop was not found or is unavailable.");

        _output.WriteLine("================ UTCID02 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID03: Product does not exist or does not belong to shop -> Throws NotFound (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID03_UpdateProduct_ProductNotFoundOrNotOwned_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateProductRequest
        {
            CategoryId = _newCategoryId,
            Name = "Updated Bread"
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _nonExistentProductId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(_userId, _nonExistentProductId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Product was not found or is unavailable.");

        _output.WriteLine("================ UTCID03 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID04: New category is not active or does not exist -> Throws BadRequest (Abnormal - A)
    // Business Rules: BR-074
    // =========================================================================
    [Fact]
    public async Task UTCID04_UpdateProduct_CategoryNotActiveOrNotFound_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new UpdateProductRequest
        {
            CategoryId = _inactiveCategoryId,
            Name = "Updated Bread"
        };

        var product = new Product
        {
            Id = _productId,
            ShopId = _shopId,
            CategoryId = _originalCategoryId,
            Name = "Original Bread",
            Category = new Category { Id = _originalCategoryId, Name = "Original Category" }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.ActiveCategoryExistsAsync(_inactiveCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(_userId, _productId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("An active category is required.");

        _output.WriteLine("================ UTCID04 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 Bad Request");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID05: Product name is empty -> Validator rejects (Boundary - B)
    // Business Rules: BR-074
    // =========================================================================
    [Fact]
    public async Task UTCID05_UpdateProduct_EmptyName_ValidationFails_Boundary()
    {
        // Arrange
        var request = new UpdateProductRequest
        {
            CategoryId = _newCategoryId,
            Name = ""
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID05", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    private void PrintSuccess<T>(string tcid, T result, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
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
