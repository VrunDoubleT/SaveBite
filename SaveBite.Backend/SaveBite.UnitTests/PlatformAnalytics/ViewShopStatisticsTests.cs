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
/// Unit Tests for Feature: View Shop Statistics
/// Service: PlatformAnalyticsService.GetShopStatisticsAsync
/// Function Code: UT-131
/// Sheet Name: UT_ViewShopStatistics
/// </summary>
public class ViewShopStatisticsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformAnalyticsRepository> _repositoryMock = new();
    private readonly PlatformAnalyticsService _service;

    public ViewShopStatisticsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformAnalyticsService(_repositoryMock.Object, TimeProvider.System);
    }

    // =========================================================================
    // TCID01: Valid Request -> Returns Shop Metrics and Status Breakdowns (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetShopStatistics_ValidRequest_ReturnsShopMetrics_Normal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest { Preset = "today" };

        var shopCounts = new ShopCountsRow(
            Total: 50,
            New: 3,
            NewApplications: 5,
            ShopsWithActiveDeals: 20,
            ShopsWithCompletedOrders: 30,
            ShopStatuses: new List<KeyValuePair<ShopStatus, int>>
            {
                new(ShopStatus.Active, 45),
                new(ShopStatus.Paused, 3),
                new(ShopStatus.Suspended, 2)
            },
            ApplicationStatuses: new List<KeyValuePair<ShopApplicationStatus, int>>
            {
                new(ShopApplicationStatus.Approved, 40),
                new(ShopApplicationStatus.Pending, 5),
                new(ShopApplicationStatus.Rejected, 5)
            }
        );

        _repositoryMock
            .Setup(r => r.GetShopCountsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shopCounts);

        // Act
        var result = await _service.GetShopStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalShops.Should().Be(50);
        result.NewShopsInPeriod.Should().Be(3);
        result.NewApplicationsInPeriod.Should().Be(5);
        result.ActiveShops.Should().Be(45);
        result.PausedShops.Should().Be(3);
        result.SuspendedShops.Should().Be(2);
        result.ShopsWithActiveDeals.Should().Be(20);
        result.ShopsWithCompletedOrdersInPeriod.Should().Be(30);
        result.ShopStatusBreakdown.Should().NotBeEmpty();
        result.ApplicationStatusBreakdown.Should().NotBeEmpty();

        _output.WriteLine($"[TCID01 PASSED] 200 OK - 50 total shops (45 Active, 2 Suspended).");
    }
}
