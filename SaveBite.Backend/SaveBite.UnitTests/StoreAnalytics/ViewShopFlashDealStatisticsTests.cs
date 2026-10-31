using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreAnalytics;

/// <summary>
/// Unit Tests for Feature: View Shop Flash Deal Statistics
/// Service: StoreAnalyticsService.GetFlashDealStatisticsAsync
/// Function Code: UT-124
/// Sheet Name: UT_ViewShopFlashDealStatistics
/// </summary>
public class ViewShopFlashDealStatisticsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IStoreAnalyticsRepository> _repositoryMock = new();
    private readonly StoreAnalyticsService _service;

    public static readonly Guid ValidOwnerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidShopId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public ViewShopFlashDealStatisticsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new StoreAnalyticsService(_repositoryMock.Object, TimeProvider.System);
    }

    private Shop CreateValidShop() => new()
    {
        Id = ValidShopId,
        OwnerUserId = ValidOwnerUserId,
        Name = "Bánh Mì Ngon",
        Status = ShopStatus.Active
    };

    // =========================================================================
    // TCID01: Valid Owner, Returns Deal State Counts & Sales Metrics (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetFlashDealStatistics_ValidOwner_ReturnsDealMetrics_Normal()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest { Preset = "today" };

        var dealCounts = new DealStateCountsRow(
            Created: 5,
            OnSaleNow: 2,
            PendingNow: 1,
            HiddenNow: 0,
            CancelledEarly: 1,
            EndedOnSchedule: 1,
            SoldOut: 1,
            AllocatedUnits: 50,
            SoldUnits: 40,
            ReservedUnits: 5,
            OrdersWithDeals: 15,
            StatusBreakdown: new List<DealStatusCountRow> { new(FlashDealStatus.OnSale, 2), new(FlashDealStatus.Cancelled, 1) }
        );

        var dealSales = new DealSalesTotalsRow(Quantity: 40, Revenue: 600000m, CustomerSavings: 200000m);
        var topDeals = new List<TopDealRow>
        {
            new(Guid.NewGuid(), "Bánh Mì Pate", FlashDealStatus.OnSale, 20, DateTime.UtcNow, DateTime.UtcNow.AddHours(4), 18, 270000m)
        };

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetDealStateCountsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dealCounts);

        _repositoryMock
            .Setup(r => r.GetDealSalesTotalsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dealSales);

        _repositoryMock
            .Setup(r => r.GetTopDealsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(topDeals);

        // Act
        var result = await _service.GetFlashDealStatisticsAsync(ValidOwnerUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.CreatedDealsCount.Should().Be(5);
        result.OnSaleNowCount.Should().Be(2);
        result.CompletedOrderItemsQuantitySold.Should().Be(40);
        result.CompletedOrderItemsSalesAmount.Should().Be(600000m);
        result.SellThroughRatePercent.Should().Be(80.0m); // 40/50 = 80%
        result.TopPerformingDeals.Should().HaveCount(1);
        result.TopPerformingDeals[0].ProductName.Should().Be("Bánh Mì Pate");

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Deal sales: {result.CompletedOrderItemsSalesAmount:N0} VND, Sell-through: {result.SellThroughRatePercent}%.");
    }

    // =========================================================================
    // TCID02: User Has No Shop -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetFlashDealStatistics_NoOwnedShop_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest();
        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var act = () => _service.GetFlashDealStatisticsAsync(ValidOwnerUserId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _output.WriteLine($"[TCID02 PASSED] 404 NotFound thrown as expected: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: Zero Deals Created / Allocated -> Returns 0% Sell-Through (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetFlashDealStatistics_ZeroAllocatedUnits_ReturnsZeroSellThrough_Boundary()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest { Preset = "7d" };

        var dealCounts = new DealStateCountsRow(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new List<DealStatusCountRow>());
        var dealSales = new DealSalesTotalsRow(0, 0m, 0m);

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetDealStateCountsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dealCounts);

        _repositoryMock
            .Setup(r => r.GetDealSalesTotalsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dealSales);

        _repositoryMock
            .Setup(r => r.GetTopDealsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopDealRow>());

        // Act
        var result = await _service.GetFlashDealStatisticsAsync(ValidOwnerUserId, request);

        // Assert
        result.SellThroughRatePercent.Should().Be(0m);
        result.CompletedOrderItemsQuantitySold.Should().Be(0);

        _output.WriteLine("[TCID03 PASSED] Zero allocated units safely returns 0% sell-through.");
    }
}
