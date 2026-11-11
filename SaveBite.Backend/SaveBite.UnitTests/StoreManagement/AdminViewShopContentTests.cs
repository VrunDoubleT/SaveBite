using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreManagement;

/// <summary>
/// Unit Tests for Feature: View Shop Content
/// Service: ShopService.GetShopContentAsync
/// Function Code: UT-MOD-06
/// </summary>
public class AdminViewShopContentTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly ShopService _service;

    public AdminViewShopContentTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopService(_shopRepoMock.Object, _userAddressRepoMock.Object, _cloudinaryMock.Object, new Mock<IAdminAuditWriter>().Object, new Mock<INotificationQueue>().Object, TestDbContextFactory.Create(), new Mock<IShopSuspensionCleanupService>().Object, new Mock<IShopStatusEventService>().Object, new Mock<IRealtimePublisher>().Object);
    }

    [Fact]
    public async Task UTCID01_GetShopContent_WithValidShopId_ReturnsShopDetailsAndHistory()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var owner = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nguyen Van A",
            Email = "owner@test.com",
            Phone = "0901234567"
        };
        var shop = new Shop
        {
            Id = shopId,
            Name = "SaveBite Flagship",
            Description = "Quality surplus food",
            Status = ShopStatus.Active,
            OwnerUserId = owner.Id,
            Owner = owner,
            AddressLine = "123 Le Loi",
            City = "Ho Chi Minh",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var auditLogs = new List<AuditLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Action = "SHOP_STATUS_UPDATED",
                OldValuesJson = "{\"Status\":\"Paused\"}",
                NewValuesJson = "{\"Status\":\"Active\"}",
                Reason = "Re-opened after inspection",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        _shopRepoMock
            .Setup(r => r.GetShopContentAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _shopRepoMock
            .Setup(r => r.GetShopStatusHistoryAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _service.GetShopContentAsync(shopId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(shopId);
        result.Name.Should().Be("SaveBite Flagship");
        result.Owner.Should().NotBeNull();
        result.Owner!.FullName.Should().Be("Nguyen Van A");
        result.StatusHistory.Should().HaveCount(1);
        result.StatusHistory.First().FromStatus.Should().Be("Paused");
        result.StatusHistory.First().ToStatus.Should().Be("Active");
    }

    [Fact]
    public async Task UTCID02_GetShopContent_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        _shopRepoMock
            .Setup(r => r.GetShopContentAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var act = () => _service.GetShopContentAsync(shopId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Shop not found*");
    }
}
