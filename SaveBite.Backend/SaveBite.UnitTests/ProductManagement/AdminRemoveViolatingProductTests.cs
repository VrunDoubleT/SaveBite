using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.ProductManagement;

/// <summary>
/// Unit Tests for Feature: Remove Violating Product
/// Service: ProductService.RemoveProductAsync
/// Function Code: UT-MOD-03
/// </summary>
public class AdminRemoveViolatingProductTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly ProductService _service;

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public AdminRemoveViolatingProductTests(ITestOutputHelper output)
    {
        _output = output;
        var auditMock = new Mock<IAdminAuditWriter>();
        auditMock.Setup(a => a.Record(It.IsAny<AuditLog>())).Returns((AuditLog l) => l);
        _service = new ProductService(_repositoryMock.Object, _cloudinaryMock.Object, TimeProvider.System, _loggerMock.Object, auditMock.Object, TestDbContextFactory.Create(), new Mock<INotificationQueue>().Object);
    }

    [Fact]
    public async Task UTCID01_RemoveProduct_ValidActiveProduct_RemovesAndPublishesEvent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            ShopId = Guid.NewGuid(),
            Shop = new Shop { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid() },
            Name = "Expired Bread",
            Status = ProductStatus.Active,
            DeletedAt = null
        };

        var request = new RemoveViolatingProductRequest
        {
            Reason = "Product contains expired and unsafe ingredients"
        };

        _repositoryMock
            .Setup(r => r.GetProductForViolationRemovalAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.RemoveProductForViolationAndCancelDealsAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        

        // Act
        var result = await _service.RemoveProductAsync(_adminId, productId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TargetId.Should().Be(productId);
        result.Action.Should().Be("PRODUCT_REMOVED_VIOLATION");
        product.Status.Should().Be(ProductStatus.Hidden);
        product.DeletedAt.Should().NotBeNull();

        _repositoryMock.Verify(r => r.RemoveProductForViolationAndCancelDealsAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        
    }

    [Fact]
    public async Task UTCID02_RemoveProduct_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new RemoveViolatingProductRequest { Reason = "Violation reason" };

        _repositoryMock
            .Setup(r => r.GetProductForViolationRemovalAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _service.RemoveProductAsync(_adminId, productId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Product not found*");
    }

    [Fact]
    public async Task UTCID03_RemoveProduct_WhenAlreadyRemoved_ThrowsConflictException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            ShopId = Guid.NewGuid(),
            Name = "Previously Deleted Bread",
            Status = ProductStatus.Hidden,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };

        var request = new RemoveViolatingProductRequest { Reason = "Duplicate delete" };

        _repositoryMock
            .Setup(r => r.GetProductForViolationRemovalAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var act = () => _service.RemoveProductAsync(_adminId, productId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Product already removed*");
    }
}
