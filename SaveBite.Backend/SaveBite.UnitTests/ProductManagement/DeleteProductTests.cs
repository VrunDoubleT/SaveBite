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
/// Unit Tests for Feature: Delete Product
/// Service: ProductService.DeleteAsync
/// Function Code: UT_DeleteProduct
/// Sheet Name: UT_DeleteProduct
/// Business Rules: BR-073, BR-076
/// </summary>
public class DeleteProductTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly ProductService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _nonExistentProductId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public DeleteProductTests(ITestOutputHelper output)
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
    // UTCID01: Store owner deletes product -> Soft-deletes and sets status to Hidden (Normal - N)
    // Business Rules: BR-073, BR-076
    // =========================================================================
    [Fact]
    public async Task UTCID01_DeleteProduct_ActiveProduct_SetsDeletedAtAndHiddenStatus_Normal()
    {
        // Arrange
        var product = new Product
        {
            Id = _productId,
            ShopId = _shopId,
            Name = "Signature Pate Bread",
            Status = ProductStatus.Active,
            DeletedAt = null
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(_userId, _productId, CancellationToken.None);

        // Assert & Log
        _output.WriteLine("================ UTCID01 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK (Product Soft-Deleted)");
        _output.WriteLine("========================================");

        product.DeletedAt.Should().NotBeNull();
        product.Status.Should().Be(ProductStatus.Hidden);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Product is already deleted -> Returns silently without modifying (Normal - N)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID02_DeleteProduct_AlreadyDeleted_ReturnsSilently_Normal()
    {
        // Arrange
        var alreadyDeletedTime = DateTime.UtcNow.AddDays(-1);
        var product = new Product
        {
            Id = _productId,
            ShopId = _shopId,
            Name = "Signature Pate Bread",
            Status = ProductStatus.Hidden,
            DeletedAt = alreadyDeletedTime
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _service.DeleteAsync(_userId, _productId, CancellationToken.None);

        // Assert & Log
        _output.WriteLine("================ UTCID02 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK (Idempotent - Already Deleted)");
        _output.WriteLine("========================================");

        product.DeletedAt.Should().Be(alreadyDeletedTime);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: User has no shop or shop is unavailable -> Throws NotFound (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID03_DeleteProduct_NoShopAvailable_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductShopOption?)null);

        // Act
        Func<Task> act = async () => await _service.DeleteAsync(_userId, _productId, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Shop was not found or is unavailable.");

        _output.WriteLine("================ UTCID03 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID04: Product does not exist or does not belong to shop -> Throws NotFound (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID04_DeleteProduct_ProductNotFoundOrNotOwned_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _nonExistentProductId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.DeleteAsync(_userId, _nonExistentProductId, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Product was not found or is unavailable.");

        _output.WriteLine("================ UTCID04 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }
}
