using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.ProductManagement;

/// <summary>
/// Unit Tests for Feature: View Product Detail
/// Service: ProductService.GetAsync
/// Function Code: UT_ViewProductDetail
/// Sheet Name: UT_ViewProductDetail
/// Business Rules: BR-073, BR-075
/// </summary>
public class ViewProductDetailTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly ProductService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _categoryId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _productId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _nonExistentProductId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private readonly Guid _deletedProductId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public ViewProductDetailTests(ITestOutputHelper output)
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
    // UTCID01: Store owner views product details successfully (Normal - N)
    // Business Rules: BR-073, BR-075
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetProduct_StoreOwner_ReturnsProductDetails_Normal()
    {
        // Arrange
        var product = new Product
        {
            Id = _productId,
            ShopId = _shopId,
            CategoryId = _categoryId,
            Name = "Signature Pate Bread",
            Description = "Fresh artisan bread with homemade pate",
            Status = ProductStatus.Active,
            Category = new Category { Id = _categoryId, Name = "Bakery" },
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            Variants = new List<ProductVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = _productId,
                    Sku = "SKU-PATE-01",
                    OriginalPrice = 30000m,
                    DealPrice = 20000m,
                    IsActive = true
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.GetAsync(_userId, _productId, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_productId);
        result.Name.Should().Be("Signature Pate Bread");
        result.Status.Should().Be(ProductStatus.Active);
        result.CategoryName.Should().Be("Bakery");
        result.Variants.Should().HaveCount(1);
        result.Variants[0].OriginalPrice.Should().Be(30000m);
        result.Variants[0].DealPrice.Should().Be(20000m);
    }

    // =========================================================================
    // UTCID02: User has no shop or shop is unavailable -> Throws NotFound (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetProduct_NoShopAvailable_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductShopOption?)null);

        // Act
        Func<Task> act = async () => await _service.GetAsync(_userId, _productId, CancellationToken.None);

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
    public async Task UTCID03_GetProduct_ProductNotFoundOrNotOwned_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _nonExistentProductId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.GetAsync(_userId, _nonExistentProductId, CancellationToken.None);

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
    // UTCID04: Product is deleted (soft-deleted) -> Throws NotFound (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetProduct_ProductDeleted_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _deletedProductId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.GetAsync(_userId, _deletedProductId, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Product was not found or is unavailable.");

        _output.WriteLine("================ UTCID04 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
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
}
