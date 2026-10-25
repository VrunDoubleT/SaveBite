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
/// Unit Tests for Feature: Cancel Flash Deal
/// Service: FlashDealService.CancelDealAsync
/// Function Code: UT-80
/// Sheet Name: UT_CancelFlashDeal
/// </summary>
public class CancelFlashDealTests
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
    private readonly Guid _existingDealId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly Guid _nonExistentDealId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private readonly Guid _closedDealId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public CancelFlashDealTests(ITestOutputHelper output)
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

    private FlashDeal CreateSampleDeal(Guid dealId, FlashDealStatus status = FlashDealStatus.OnSale)
    {
        var now = DateTime.UtcNow;
        return new FlashDeal
        {
            Id = dealId,
            ShopId = _shopId,
            ProductId = Guid.NewGuid(),
            SaleStartTime = now.AddHours(-1),
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(3),
            Status = status,
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now.AddHours(-1)
        };
    }

    // =========================================================================
    // UTCID01: Store Owner cancels active flash deal successfully (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_CancelDeal_ActiveDeal_CancelsSuccessfully_Normal()
    {
        // Arrange
        var shop = CreateSampleShop();
        var deal = CreateSampleDeal(_existingDealId, FlashDealStatus.OnSale);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _dealRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<FlashDeal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _publisherMock
            .Setup(p => p.PublishAsync(RabbitMqRoutingKeys.FlashDealClosed, It.IsAny<FlashDealEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Func<Task> act = async () => await _service.CancelDealAsync(_ownerUserId, _existingDealId);

        // Assert & Log
        await act.Should().NotThrowAsync();
        deal.Status.Should().Be(FlashDealStatus.Cancelled);
        _dealRepoMock.Verify(r => r.UpdateAsync(deal, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(RabbitMqRoutingKeys.FlashDealClosed, It.Is<FlashDealEvent>(e => e.FlashDealId == _existingDealId), It.IsAny<CancellationToken>()), Times.Once);

        PrintSuccess("UTCID01", "Flash deal cancelled successfully.");
    }

    // =========================================================================
    // UTCID02: Deal does not exist in shop -> Throws NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID02_CancelDeal_DealNotFound_ThrowsNotFound_Abnormal()
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
        Func<Task> act = async () => await _service.CancelDealAsync(_ownerUserId, _nonExistentDealId);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID02", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Flash deal not found.");
    }

    // =========================================================================
    // UTCID03: Deal is already closed or cancelled -> Throws BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_CancelDeal_AlreadyClosedOrCancelled_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var shop = CreateSampleShop();
        var closedDeal = CreateSampleDeal(_closedDealId, FlashDealStatus.Closed);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _closedDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(closedDeal);

        // Act
        Func<Task> act = async () => await _service.CancelDealAsync(_ownerUserId, _closedDealId);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID03", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be("This flash deal is already finished or cancelled.");
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
