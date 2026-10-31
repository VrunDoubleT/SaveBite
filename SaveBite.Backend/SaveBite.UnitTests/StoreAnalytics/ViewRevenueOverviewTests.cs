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
/// Unit Tests for Feature: View Revenue Overview
/// Service: StoreAnalyticsService.GetRevenueOverviewAsync
/// Function Code: UT-122
/// Sheet Name: UT_ViewRevenueOverview
/// </summary>
public class ViewRevenueOverviewTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IStoreAnalyticsRepository> _repositoryMock = new();
    private readonly StoreAnalyticsService _service;

    public static readonly Guid ValidOwnerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid NonOwnerUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ValidShopId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public ViewRevenueOverviewTests(ITestOutputHelper output)
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
    // TCID01: Valid Owner & Orders Present -> Calculates Revenue Breakdown (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetRevenueOverview_ValidOwnerWithOrders_ReturnsRevenueMetrics_Normal()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest { Preset = "today" };

        var rows = new List<CompletedOrderFinancialRow>
        {
            new(Guid.NewGuid(), ValidShopId, 150000m, 150000m, 0m, 0m, 15000m, DateTime.UtcNow),
            new(Guid.NewGuid(), ValidShopId, 200000m, 100000m, 100000m, 50000m, 20000m, DateTime.UtcNow)
        };

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetCompletedOrderFinancialsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        // Act
        var result = await _service.GetRevenueOverviewAsync(ValidOwnerUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.CompletedOrdersCount.Should().Be(2);
        result.CollectedOnlineAmount.Should().Be(250000m);
        result.CollectedCashAmount.Should().Be(100000m);
        result.ActuallyPaidCompletedAmount.Should().Be(350000m);
        result.RefundedAmount.Should().Be(50000m);
        result.SalesRevenue.Should().Be(300000m); // Paid - Refunded (BR-114)
        result.NetPlatformFeeAmount.Should().Be(35000m);
        result.NetShopRevenue.Should().Be(265000m); // SalesRevenue - Fee
        result.AverageCompletedOrderValue.Should().Be(175000m);

        _repositoryMock.Verify(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.GetCompletedOrderFinancialsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Sales revenue: {result.SalesRevenue:N0} VND (Net: {result.NetShopRevenue:N0} VND).");
    }

    // =========================================================================
    // TCID02: User has no Owned Shop -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetRevenueOverview_UserHasNoOwnedShop_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest();

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(NonOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var act = () => _service.GetRevenueOverviewAsync(NonOwnerUserId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Contain("Shop profile not found");

        _repositoryMock.Verify(r => r.GetCompletedOrderFinancialsAsync(It.IsAny<Guid>(), It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID02 PASSED] 404 NotFound thrown as expected: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: Invalid Period Range (FromDate > ToDate) -> Throws 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetRevenueOverview_FromDateLaterThanToDate_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest
        {
            FromDate = new DateOnly(2026, 10, 31),
            ToDate = new DateOnly(2026, 10, 1)
        };

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        // Act
        var act = () => _service.GetRevenueOverviewAsync(ValidOwnerUserId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("FromDate cannot be later than ToDate");

        _output.WriteLine($"[TCID03 PASSED] 400 BadRequest thrown as expected: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: Zero Orders in Period -> Returns Zero Revenue Breakdown (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID04_GetRevenueOverview_ZeroOrders_ReturnsZeroAverages_Boundary()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest { Preset = "7d" };

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetCompletedOrderFinancialsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletedOrderFinancialRow>());

        // Act
        var result = await _service.GetRevenueOverviewAsync(ValidOwnerUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.CompletedOrdersCount.Should().Be(0);
        result.SalesRevenue.Should().Be(0m);
        result.ActuallyPaidCompletedAmount.Should().Be(0m);
        result.AverageCompletedOrderValue.Should().Be(0m);

        _output.WriteLine("[TCID04 PASSED] Boundary test passed: 0 orders cleanly handled without divide by zero.");
    }
}
