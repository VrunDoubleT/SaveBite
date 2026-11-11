using FluentAssertions;
using Moq;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreManagement;

/// <summary>
/// Unit Tests for Feature: View Shops
/// Service: ShopService.GetShopListAsync
/// Function Code: UT-MOD-05
/// </summary>
public class AdminViewShopsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly ShopService _service;

    public AdminViewShopsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopService(_shopRepoMock.Object, _userAddressRepoMock.Object, _cloudinaryMock.Object, new Mock<IAdminAuditWriter>().Object, new Mock<INotificationQueue>().Object, TestDbContextFactory.Create(), new Mock<IShopSuspensionCleanupService>().Object, new Mock<IShopStatusEventService>().Object, new Mock<IRealtimePublisher>().Object);
    }

    [Fact]
    public async Task UTCID01_GetShopList_WithValidKeyword_ReturnsFilteredShops()
    {
        // Arrange
        var owner = new User { Id = Guid.NewGuid(), FullName = "Shop Owner A" };
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "SaveBite Bakery",
            Description = "Fresh discount bread",
            City = "Ho Chi Minh",
            Status = ShopStatus.Active,
            OwnerUserId = owner.Id,
            Owner = owner,
            CreatedAt = DateTime.UtcNow
        };

        _shopRepoMock
            .Setup(r => r.GetShopListAsync("Bakery", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Shop> { shop }, 1));

        // Act
        var result = await _service.GetShopListAsync("Bakery", 1, 20, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("SaveBite Bakery");
        result.Items.First().OwnerName.Should().Be("Shop Owner A");
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task UTCID02_GetShopList_WithoutKeyword_ReturnsAllShops()
    {
        // Arrange
        var owner = new User { Id = Guid.NewGuid(), FullName = "Owner B" };
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "Coffee House",
            Status = ShopStatus.Paused,
            OwnerUserId = owner.Id,
            Owner = owner,
            CreatedAt = DateTime.UtcNow
        };

        _shopRepoMock
            .Setup(r => r.GetShopListAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Shop> { shop }, 1));

        // Act
        var result = await _service.GetShopListAsync(null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Status.Should().Be("Paused");
    }

    [Fact]
    public async Task UTCID03_GetShopList_InvalidPageOrSize_ClampsPagination()
    {
        // Arrange
        _shopRepoMock
            .Setup(r => r.GetShopListAsync(null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Shop>(), 0));

        // Act
        var result = await _service.GetShopListAsync(null, -1, 500, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _shopRepoMock.Verify(r => r.GetShopListAsync(null, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }
}
