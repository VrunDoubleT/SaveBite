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
/// Unit Tests for Feature: View Revenue and Order Statistics
/// Service: PlatformAnalyticsService.GetRevenueAndOrderStatisticsAsync
/// Function Code: UT-132
/// Sheet Name: UT_ViewRevenueAndOrderStatistics
/// </summary>
public class ViewRevenueAndOrderStatisticsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformAnalyticsRepository> _repositoryMock = new();
    private readonly PlatformAnalyticsService _service;

    public ViewRevenueAndOrderStatisticsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformAnalyticsService(_repositoryMock.Object, TimeProvider.System);
    }

    // =========================================================================
    // TCID01: Orders & Financials Present -> Returns Platform Revenue and Order Metrics (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetRevenueAndOrderStatistics_ValidRequest_ReturnsPlatformFinancials_Normal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest { Preset = "today" };
        var financials = new List<CompletedOrderFinancialRow>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), 200000m, 150000m, 50000m, 0m, 16000m, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), 300000m, 200000m, 100000m, 50000m, 24000m, DateTime.UtcNow)
        };

        var statusRows = new List<OrderStatusCountRow>
        {
            new(OrderStatus.Completed, 2, 0, 500000m),
            new(OrderStatus.Cancelled, 1, 0, 100000m)
        };

        _repositoryMock
            .Setup(r => r.GetCompletedOrderFinancialsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(financials);

        _repositoryMock
            .Setup(r => r.GetOrderStatusCountsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusRows);

        // Act
        var result = await _service.GetRevenueAndOrderStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalOrdersInPeriod.Should().Be(3);
        result.CompletedOrdersInPeriod.Should().Be(2);
        result.GrossMerchandiseValue.Should().Be(500000m); // Cancelled excluded
        result.CompletedOrdersPaidRevenue.Should().Be(500000m); // (150+50) + (200+100)
        result.CollectedOnlineAmount.Should().Be(350000m);
        result.CollectedCashAmount.Should().Be(150000m);
        result.TotalRefundedAmount.Should().Be(50000m);
        result.NetPlatformOrderRevenue.Should().Be(450000m); // Paid - Refunded
        result.AverageCompletedOrderValue.Should().Be(250000m);
        result.OrderCompletionRatePercent.Should().Be(66.7m); // 2/3 = 66.7%
        result.OrderStatusBreakdown.Should().NotBeEmpty();

        _output.WriteLine($"[TCID01 PASSED] 200 OK - GMV: {result.GrossMerchandiseValue:N0} VND, Net: {result.NetPlatformOrderRevenue:N0} VND.");
    }
}
