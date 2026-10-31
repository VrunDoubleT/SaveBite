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
/// Unit Tests for Feature: View Order Statistics
/// Service: StoreAnalyticsService.GetOrderStatisticsAsync
/// Function Code: UT-123
/// Sheet Name: UT_ViewOrderStatistics
/// </summary>
public class ViewOrderStatisticsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IStoreAnalyticsRepository> _repositoryMock = new();
    private readonly StoreAnalyticsService _service;

    public static readonly Guid ValidOwnerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidShopId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public ViewOrderStatisticsTests(ITestOutputHelper output)
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
    // TCID01: Valid Owner, Returns Grouped Order Counts & Breakdown (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetOrderStatistics_ValidOwner_ReturnsOrderCountsAndRates_Normal()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest { Preset = "today" };

        var statusRows = new List<OrderStatusCountRow>
        {
            new(OrderStatus.Completed, 10, 1, 1500000m),
            new(OrderStatus.Cancelled, 2, 0, 200000m),
            new(OrderStatus.Pending, 1, 0, 100000m),
            new(OrderStatus.NoShow, 1, 1, 80000m)
        };

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetOrderStatusCountsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusRows);

        // Act
        var result = await _service.GetOrderStatisticsAsync(ValidOwnerUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.TotalOrders.Should().Be(14);
        result.CompletedOrders.Should().Be(10);
        result.CancelledOrRejectedOrders.Should().Be(2);
        result.PendingOrders.Should().Be(1);
        result.NoShowOrders.Should().Be(1);
        result.LateArrivalOrders.Should().Be(2);
        result.CompletionRatePercent.Should().Be(71.4m); // 10/14 = 71.4%
        result.CancellationRatePercent.Should().Be(14.3m); // 2/14 = 14.3%
        result.StatusBreakdown.Should().NotBeEmpty();

        _output.WriteLine($"[TCID01 PASSED] 200 OK - 14 total orders, 71.4% completed, 14.3% cancelled.");
    }

    // =========================================================================
    // TCID02: User not owning any Shop -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetOrderStatistics_NoOwnedShop_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest();
        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var act = () => _service.GetOrderStatisticsAsync(ValidOwnerUserId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _output.WriteLine($"[TCID02 PASSED] 404 NotFound thrown as expected: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: Zero Orders in Period -> Returns Zero Rates (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetOrderStatistics_ZeroOrders_ReturnsZeroRates_Boundary()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest { Preset = "month" };

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetOrderStatusCountsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderStatusCountRow>());

        // Act
        var result = await _service.GetOrderStatisticsAsync(ValidOwnerUserId, request);

        // Assert
        result.TotalOrders.Should().Be(0);
        result.CompletedOrders.Should().Be(0);
        result.CompletionRatePercent.Should().Be(0m);
        result.CancellationRatePercent.Should().Be(0m);

        _output.WriteLine("[TCID03 PASSED] 0 orders boundary handled without exception.");
    }
}
