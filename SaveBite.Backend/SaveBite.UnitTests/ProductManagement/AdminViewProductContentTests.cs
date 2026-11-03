using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.ProductManagement;

/// <summary>
/// Unit Tests for Feature: View Product Content
/// Service: ProductService.GetProductContentAsync
/// Function Code: UT-MOD-02
/// </summary>
public class AdminViewProductContentTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly ProductService _service;

    public AdminViewProductContentTests(ITestOutputHelper output)
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
    public async Task UTCID01_GetProductContent_WithValidId_ReturnsContentDetail()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var shop = new Shop { Id = Guid.NewGuid(), Name = "Pastry Paradise" };
        var category = new Category { Id = Guid.NewGuid(), Name = "Pastry" };
        var product = new Product
        {
            Id = productId,
            Name = "Croissant",
            Description = "Butter croissant",
            ShopId = shop.Id,
            Shop = shop,
            CategoryId = category.Id,
            Category = category,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetProductContentAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.GetProductContentAsync(productId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be("Croissant");
        result.ShopName.Should().Be("Pastry Paradise");
        result.CategoryName.Should().Be("Pastry");
    }

    [Fact]
    public async Task UTCID02_GetProductContent_WithEmptyId_ThrowsBadRequestException()
    {
        // Act
        var act = () => _service.GetProductContentAsync(Guid.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Product ID is invalid*");
    }

    [Fact]
    public async Task UTCID03_GetProductContent_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetProductContentAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _service.GetProductContentAsync(productId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Product not found*");
    }
}
