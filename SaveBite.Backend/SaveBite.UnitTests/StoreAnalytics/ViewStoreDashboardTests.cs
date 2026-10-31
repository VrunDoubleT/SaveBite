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
/// Unit Tests for Feature: View Store Dashboard
/// Service: StoreAnalyticsService.GetDashboardAsync
/// Function Code: UT-126
/// Sheet Name: UT_ViewStoreDashboard
/// </summary>
public class ViewStoreDashboardTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IStoreAnalyticsRepository> _repositoryMock = new();
    private readonly StoreAnalyticsService _service;

    public static readonly Guid ValidOwnerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidShopId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public ViewStoreDashboardTests(ITestOutputHelper output)
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
    // TCID01: Valid Owner -> Returns Both Selected Period and All-Time Snapshots (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetDashboard_ValidOwner_ReturnsSelectedAndAllTimeSnapshots_Normal()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest { Preset = "today" };

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetCompletedOrderFinancialsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletedOrderFinancialRow>());

        _repositoryMock
            .Setup(r => r.GetOrderStatusCountsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderStatusCountRow>());

        _repositoryMock
            .Setup(r => r.GetDealStateCountsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DealStateCountsRow(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new List<DealStatusCountRow>()));

        _repositoryMock
            .Setup(r => r.GetDealSalesTotalsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DealSalesTotalsRow(0, 0m, 0m));

        _repositoryMock
            .Setup(r => r.GetTopDealsAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopDealRow>());

        _repositoryMock
            .Setup(r => r.GetSucceededPaymentsByMethodAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentMethodAmountRow>());

        // Act
        var result = await _service.GetDashboardAsync(ValidOwnerUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.ShopId.Should().Be(ValidShopId);
        result.ShopName.Should().Be("Bánh Mì Ngon");
        result.Period.Should().NotBeNull();
        result.Selected.Should().NotBeNull();
        result.Overall.Should().NotBeNull();

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Dashboard loaded for Shop {result.ShopName} with Selected and Overall snapshots.");
    }

    // =========================================================================
    // TCID02: User has no shop -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetDashboard_NoShop_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var act = () => _service.GetDashboardAsync(ValidOwnerUserId, new AnalyticsPeriodRequest());

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _output.WriteLine($"[TCID02 PASSED] 404 NotFound properly thrown: {ex.Which.Message}");
    }
}
