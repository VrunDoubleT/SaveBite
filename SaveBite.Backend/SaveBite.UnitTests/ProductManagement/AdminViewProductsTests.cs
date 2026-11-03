using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.ProductManagement;

/// <summary>
/// Unit Tests for Feature: View Products for Admin
/// Service: ProductService.GetProductListAsync
/// Function Code: UT-MOD-01
/// </summary>
public class AdminViewProductsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly ProductService _service;

    public AdminViewProductsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ProductService(
            _repositoryMock.Object,
            _cloudinaryMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            _publisherMock.Object,
            null!
        );
    }

    [Fact]
    public async Task UTCID01_GetProductList_WithValidKeyword_ReturnsPagedProducts()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "Bakery" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Bake Shop" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Banh Mi Cha",
            ShopId = shop.Id,
            Shop = shop,
            CategoryId = category.Id,
            Category = category,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var productList = new List<Product> { product };
        _repositoryMock
            .Setup(r => r.GetProductListAsync("Banh Mi", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((productList, 1));

        // Act
        var result = await _service.GetProductListAsync("Banh Mi", 1, 20, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Banh Mi Cha");
        result.Items.First().ShopName.Should().Be("Bake Shop");
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task UTCID02_GetProductList_WithoutKeyword_ReturnsAllProducts()
    {
        // Arrange
        var category = new Category { Id = Guid.NewGuid(), Name = "Drinks" };
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Tea House" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Tra Dao",
            ShopId = shop.Id,
            Shop = shop,
            CategoryId = category.Id,
            Category = category,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetProductListAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product> { product }, 1));

        // Act
        var result = await _service.GetProductListAsync(null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task UTCID03_GetProductList_InvalidPagination_ClampsParameters()
    {
        // Arrange - page <= 0 and pageSize > 100
        _repositoryMock
            .Setup(r => r.GetProductListAsync(null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product>(), 0));

        // Act
        var result = await _service.GetProductListAsync(null, -5, 500, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _repositoryMock.Verify(r => r.GetProductListAsync(null, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }
}
