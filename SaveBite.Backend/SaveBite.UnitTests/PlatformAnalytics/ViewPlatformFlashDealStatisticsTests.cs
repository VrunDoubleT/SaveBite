using FluentAssertions;
using Moq;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.PlatformAnalytics;

/// <summary>
/// Unit Tests for Feature: View Platform Flash Deal Statistics
/// Service: PlatformAnalyticsService.GetFlashDealStatisticsAsync
/// Function Code: UT-133
/// Sheet Name: UT_ViewPlatformFlashDealStatistics
/// </summary>
public class ViewPlatformFlashDealStatisticsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformAnalyticsRepository> _repositoryMock = new();
    private readonly PlatformAnalyticsService _service;

    public ViewPlatformFlashDealStatisticsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformAnalyticsService(_repositoryMock.Object, TimeProvider.System);
    }

    // =========================================================================
    // TCID01: Valid Request -> Returns Platform Flash Deal Impact Metrics (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetFlashDealStatistics_ValidRequest_ReturnsImpactMetrics_Normal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest { Preset = "today" };

        var dealCounts = new DealStateCountsRow(
            Created: 10,
            OnSaleNow: 4,
            PendingNow: 2,
            HiddenNow: 1,
            CancelledEarly: 1,
            EndedOnSchedule: 2,
            SoldOut: 2,
            AllocatedUnits: 100,
            SoldUnits: 75,
            ReservedUnits: 10,
            OrdersWithDeals: 30,
            StatusBreakdown: new List<DealStatusCountRow> { new(FlashDealStatus.OnSale, 4) }
        );

        var salesTotals = new DealSalesTotalsRow(Quantity: 75, Revenue: 1500000m, CustomerSavings: 500000m);

        _repositoryMock
            .Setup(r => r.GetDealStateCountsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dealCounts);

        _repositoryMock
            .Setup(r => r.GetDealSalesTotalsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesTotals);

        // Act
        var result = await _service.GetFlashDealStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CreatedDealsCount.Should().Be(10);
        result.OnSaleNowCount.Should().Be(4);
        result.TotalAllocatedUnits.Should().Be(100);
        result.TotalSoldUnitsFromCompletedOrders.Should().Be(75);
        result.CompletedDealSalesAmount.Should().Be(1500000m);
        result.CustomerEstimatedSavingsAmount.Should().Be(500000m);
        result.SellThroughRatePercent.Should().Be(75.0m);
        result.OrdersWithDealsCount.Should().Be(30);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - 75 items rescued, Customer savings: {result.CustomerEstimatedSavingsAmount:N0} VND.");
    }
}
