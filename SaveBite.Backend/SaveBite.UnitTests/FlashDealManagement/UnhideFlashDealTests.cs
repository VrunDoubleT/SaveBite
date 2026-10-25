using System.Net;
using System.Text.Json;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FlashDealManagement;

/// <summary>
/// Unit Tests for Feature: Unhide Flash Deal
/// Service: FlashDealService.UnhideDealAsync
/// Function Code: UT-81
/// Sheet Name: UT_UnhideFlashDeal
/// </summary>
public class UnhideFlashDealTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFlashDealRedisService> _redisMock = new();
    private readonly Mock<IFlashDealRepository> _dealRepoMock = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly Mock<ILogger<FlashDealService>> _loggerMock = new();
    private readonly FlashDealService _service;

    // Fixed Test Identifiers
    private readonly Guid _ownerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _existingHiddenDealId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly Guid _nonExistentDealId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private readonly Guid _notHiddenDealId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private readonly Guid _expiredHiddenDealId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public UnhideFlashDealTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FlashDealService(
            _redisMock.Object,
            _dealRepoMock.Object,
            _userAddressRepoMock.Object,
            _shopRepoMock.Object,
            _staffRepoMock.Object,
            _backgroundJobClientMock.Object,
            _publisherMock.Object,
            _loggerMock.Object
        );
    }

    private Shop CreateSampleShop()
    {
        return new Shop
        {
            Id = _shopId,
            OwnerUserId = _ownerUserId,
            Name = "SaveBite Bakery",
            AddressLine = "123 Le Loi Street",
            Latitude = 10.7769,
            Longitude = 106.7009,
            Status = ShopStatus.Active
        };
    }

    private FlashDeal CreateSampleDeal(Guid dealId, FlashDealStatus status, DateTime saleStartTime, DateTime orderEndTime)
    {
        var now = DateTime.UtcNow;
        return new FlashDeal
        {
            Id = dealId,
            ShopId = _shopId,
            ProductId = Guid.NewGuid(),
            SaleStartTime = saleStartTime,
            OrderEndTime = orderEndTime,
            ShopClosingTime = orderEndTime.AddHours(1),
            Status = status,
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-1)
        };
    }

    // =========================================================================
    // UTCID01: Store Owner unhides a hidden deal successfully (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_UnhideDeal_ValidHiddenDeal_RestoresSuccessfully_Normal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var shop = CreateSampleShop();
        var deal = CreateSampleDeal(_existingHiddenDealId, FlashDealStatus.Hidden, now.AddHours(-1), now.AddHours(2));

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _existingHiddenDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _dealRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<FlashDeal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _publisherMock
            .Setup(p => p.PublishAsync(RabbitMqRoutingKeys.FlashDealOpened, It.IsAny<FlashDealEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Func<Task> act = async () => await _service.UnhideDealAsync(_ownerUserId, _existingHiddenDealId);

        // Assert & Log
        await act.Should().NotThrowAsync();
        deal.Status.Should().Be(FlashDealStatus.OnSale);
        _dealRepoMock.Verify(r => r.UpdateAsync(deal, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(RabbitMqRoutingKeys.FlashDealOpened, It.Is<FlashDealEvent>(e => e.FlashDealId == _existingHiddenDealId), It.IsAny<CancellationToken>()), Times.Once);

        PrintSuccess("UTCID01", "Hidden flash deal restored to OnSale successfully.");
    }

    // =========================================================================
    // UTCID02: Deal does not exist in shop -> Throws NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID02_UnhideDeal_DealNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var shop = CreateSampleShop();

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _nonExistentDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlashDeal?)null);

        // Act
        Func<Task> act = async () => await _service.UnhideDealAsync(_ownerUserId, _nonExistentDealId);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID02", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Flash deal not found.");
    }

    // =========================================================================
    // UTCID03: Deal is not in Hidden status -> Throws BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_UnhideDeal_DealNotHidden_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var shop = CreateSampleShop();
        var notHiddenDeal = CreateSampleDeal(_notHiddenDealId, FlashDealStatus.OnSale, now.AddHours(-1), now.AddHours(2));

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _notHiddenDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notHiddenDeal);

        // Act
        Func<Task> act = async () => await _service.UnhideDealAsync(_ownerUserId, _notHiddenDealId);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID03", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be("Only hidden deals can be restored to visible status.");
    }

    // =========================================================================
    // UTCID04: Hidden deal has already expired -> Throws BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_UnhideDeal_DealAlreadyExpired_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var shop = CreateSampleShop();
        var expiredDeal = CreateSampleDeal(_expiredHiddenDealId, FlashDealStatus.Hidden, now.AddHours(-3), now.AddHours(-1)); // Ended 1h ago!

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _expiredHiddenDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredDeal);

        // Act
        Func<Task> act = async () => await _service.UnhideDealAsync(_ownerUserId, _expiredHiddenDealId);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID04", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be("This deal has already expired and cannot be restored.");
    }

    private void PrintSuccess(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }

    private void PrintError(string tcid, int statusCode, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {statusCode}");
        _output.WriteLine($"ERROR: {message}");
        _output.WriteLine("========================================");
    }
}
