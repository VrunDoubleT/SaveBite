using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreManagement;

/// <summary>
/// Unit Tests for Feature: Update Shop Status
/// Service: ShopService.UpdateShopStatusAsync
/// Function Code: UT-MOD-07
/// </summary>
public class AdminUpdateShopStatusTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly ShopService _service;

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public AdminUpdateShopStatusTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopService(_shopRepoMock.Object, _userAddressRepoMock.Object, _cloudinaryMock.Object, new Mock<IAdminAuditWriter>().Object, new Mock<INotificationQueue>().Object, TestDbContextFactory.Create(), new Mock<IShopSuspensionCleanupService>().Object, new Mock<IShopStatusEventService>().Object, new Mock<IRealtimePublisher>().Object, new Mock<IPayOsCredentialsValidator>().Object, new Mock<IPayOsCredentialProtector>().Object);
    }

    [Fact]
    public async Task UTCID01_UpdateStatus_ActiveToSuspended_WithReason_Succeeds()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var shop = new Shop
        {
            Id = shopId,
            Name = "Violating Shop",
            Status = ShopStatus.Active
        };

        var request = new UpdateShopModerationStatusRequest
        {
            Status = "Suspended",
            Reason = "Multiple health code violations reported"
        };

        _shopRepoMock
            .Setup(r => r.GetShopForStatusUpdateAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _shopRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        

        // Act
        var result = await _service.UpdateShopStatusAsync(_adminId, shopId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TargetId.Should().Be(shopId);
        result.Action.Should().Be("SHOP_STATUS_UPDATED");
        shop.Status.Should().Be(ShopStatus.Suspended);

        _shopRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
    }

    [Fact]
    public async Task UTCID02_UpdateStatus_WhenShopNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var request = new UpdateShopModerationStatusRequest { Status = "Paused", Reason = "Temporary pause" };

        _shopRepoMock
            .Setup(r => r.GetShopForStatusUpdateAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var act = () => _service.UpdateShopStatusAsync(_adminId, shopId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Shop not found*");
    }

    [Fact]
    public async Task UTCID03_UpdateStatus_ToSameStatus_ThrowsConflictException()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var shop = new Shop { Id = shopId, Status = ShopStatus.Active };
        var request = new UpdateShopModerationStatusRequest { Status = "Active" };

        _shopRepoMock
            .Setup(r => r.GetShopForStatusUpdateAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        // Act
        var act = () => _service.UpdateShopStatusAsync(_adminId, shopId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Shop already has this status*");
    }

    [Fact]
    public async Task UTCID04_UpdateStatus_InvalidTransition_ThrowsBadRequestException()
    {
        // Arrange - Suspended can only go to Active, not Paused
        var shopId = Guid.NewGuid();
        var shop = new Shop { Id = shopId, Status = ShopStatus.Suspended };
        var request = new UpdateShopModerationStatusRequest { Status = "Paused", Reason = "Invalid transition" };

        _shopRepoMock
            .Setup(r => r.GetShopForStatusUpdateAsync(shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        // Act
        var act = () => _service.UpdateShopStatusAsync(_adminId, shopId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Cannot change shop status from Suspended to Paused*");
    }
}
