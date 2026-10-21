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
/// Unit Tests for Feature: Update Variant
/// Service: ProductService.UpdateVariantAsync & UpdateProductVariantRequestValidator
/// Function Code: UT_UpdateVariant
/// Sheet Name: UT_UpdateVariant
/// Business Rules: BR-073, BR-076, BR-081, BR-183
/// </summary>
public class UpdateVariantTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly ProductService _service;
    private readonly UpdateProductVariantRequestValidator _validator = new();

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _variantId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _secondVariantId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly Guid _nonExistentVariantId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public UpdateVariantTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ProductService(
            _repositoryMock.Object,
            _cloudinaryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private Product CreateSampleProduct(bool hasMultipleActiveVariants = false)
    {
        var categoryId = Guid.NewGuid();
        var product = new Product
        {
            Id = _productId,
            ShopId = _shopId,
            CategoryId = categoryId,
            Name = "Signature Pate Bread",
            Status = ProductStatus.Active,
            Category = new Category { Id = categoryId, Name = "Bakery" },
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            Variants = new List<ProductVariant>
            {
                new()
                {
                    Id = _variantId,
                    ProductId = _productId,
                    Sku = "SKU-01",
                    OriginalPrice = 30000m,
                    DealPrice = 20000m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        };

        if (hasMultipleActiveVariants)
        {
            product.Variants.Add(new ProductVariant
            {
                Id = _secondVariantId,
                ProductId = _productId,
                Sku = "SKU-02",
                OriginalPrice = 35000m,
                DealPrice = 25000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }

        return product;
    }

    // =========================================================================
    // UTCID01: Store owner updates variant prices and status successfully (Normal - N)
    // Business Rules: BR-073, BR-081
    // =========================================================================
    [Fact]
    public async Task UTCID01_UpdateVariant_ValidPrices_UpdatesSuccessfully_Normal()
    {
        // Arrange
        var product = CreateSampleProduct(hasMultipleActiveVariants: true);
        var request = new UpdateProductVariantRequest
        {
            OriginalPrice = 40000m,
            DealPrice = 28000m,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.GetMaxActiveFlashDealPriceForVariantAsync(_variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateVariantAsync(_userId, _productId, _variantId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        var updatedVariant = product.Variants.Single(v => v.Id == _variantId);
        updatedVariant.OriginalPrice.Should().Be(40000m);
        updatedVariant.DealPrice.Should().Be(28000m);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Deactivating the only active variant of active product -> Throws BadRequest (Abnormal - A)
    // Business Rules: BR-183
    // =========================================================================
    [Fact]
    public async Task UTCID02_UpdateVariant_DeactivateOnlyActiveVariant_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var product = CreateSampleProduct(hasMultipleActiveVariants: false);
        var request = new UpdateProductVariantRequest
        {
            OriginalPrice = 30000m,
            DealPrice = 20000m,
            IsActive = false
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        Func<Task> act = async () => await _service.UpdateVariantAsync(_userId, _productId, _variantId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("An active product must have at least one active variant.");

        _output.WriteLine("================ UTCID02 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 Bad Request");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID03: Original price <= active flash deal price -> Throws BadRequest (Abnormal - A)
    // Business Rules: BR-081
    // =========================================================================
    [Fact]
    public async Task UTCID03_UpdateVariant_OriginalPriceLessThanActiveFlashDeal_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var product = CreateSampleProduct(hasMultipleActiveVariants: true);
        var request = new UpdateProductVariantRequest
        {
            OriginalPrice = 25000m,
            DealPrice = 18000m,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.GetMaxActiveFlashDealPriceForVariantAsync(_variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(26000m); // active flash deal price is higher than proposed original price

        // Act
        Func<Task> act = async () => await _service.UpdateVariantAsync(_userId, _productId, _variantId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Contain("Original price cannot be less than or equal to the deal price");

        _output.WriteLine("================ UTCID03 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 Bad Request");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID04: Variant not found on product -> Throws NotFound (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID04_UpdateVariant_VariantNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var product = CreateSampleProduct(hasMultipleActiveVariants: true);
        var request = new UpdateProductVariantRequest
        {
            OriginalPrice = 40000m,
            DealPrice = 28000m,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        Func<Task> act = async () => await _service.UpdateVariantAsync(_userId, _productId, _nonExistentVariantId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Product variant was not found.");

        _output.WriteLine("================ UTCID04 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID05: Deal price >= original price -> Validator rejects (Boundary - B)
    // Business Rules: BR-081
    // =========================================================================
    [Fact]
    public async Task UTCID05_UpdateVariant_DealPriceGteOriginalPrice_ValidationFails_Boundary()
    {
        // Arrange
        var request = new UpdateProductVariantRequest
        {
            OriginalPrice = 30000m,
            DealPrice = 35000m,
            IsActive = true
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID05", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "DealPrice");
    }

    // =========================================================================
    // UTCID06: Original price <= 0 -> Validator rejects (Boundary - B)
    // Business Rules: BR-081
    // =========================================================================
    [Fact]
    public async Task UTCID06_UpdateVariant_OriginalPriceZero_ValidationFails_Boundary()
    {
        // Arrange
        var request = new UpdateProductVariantRequest
        {
            OriginalPrice = 0m,
            DealPrice = 0m,
            IsActive = true
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID06", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "OriginalPrice");
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
