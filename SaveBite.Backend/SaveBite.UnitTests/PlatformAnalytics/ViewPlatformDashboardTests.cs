using FluentAssertions;
using Moq;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.PlatformAnalytics;

/// <summary>
/// Unit Tests for Feature: View Platform Dashboard
/// Service: PlatformAnalyticsService.GetDashboardAsync
/// Function Code: UT-135
/// Sheet Name: UT_ViewPlatformDashboard
/// </summary>
public class ViewPlatformDashboardTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformAnalyticsRepository> _repositoryMock = new();
    private readonly PlatformAnalyticsService _service;

    public ViewPlatformDashboardTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformAnalyticsService(_repositoryMock.Object, TimeProvider.System);
    }

    // =========================================================================
    // TCID01: Valid Request -> Returns Unified Platform Dashboard (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetDashboard_ValidRequest_ReturnsSelectedAndAllTimeSnapshots_Normal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest { Preset = "today" };

        _repositoryMock
            .Setup(r => r.GetCurrentFeeConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformFeeConfig { FeeRate = 0.05m, EffectiveFrom = DateTime.UtcNow.AddDays(-1) });

        _repositoryMock
            .Setup(r => r.GetFeeAccrualAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeeAccrualRow(0, 0m, 0m));

        _repositoryMock
            .Setup(r => r.GetFeeStatementSummaryAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeeStatementSummaryRow(0m, 0m, new List<StatementStatusRow>(), new List<FeeStatementRow>()));

        _repositoryMock
            .Setup(r => r.GetUserCountsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserCountsRow(0, 0, 0, 0, 0, 0, 0, 0, new List<KeyValuePair<UserRole, int>>(), new List<KeyValuePair<string, int>>(), 0, new List<DateTime>()));

        _repositoryMock
            .Setup(r => r.GetShopCountsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShopCountsRow(0, 0, 0, 0, 0, new List<KeyValuePair<ShopStatus, int>>(), new List<KeyValuePair<ShopApplicationStatus, int>>()));

        _repositoryMock
            .Setup(r => r.GetCompletedOrderFinancialsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CompletedOrderFinancialRow>());

        _repositoryMock
            .Setup(r => r.GetOrderStatusCountsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderStatusCountRow>());

        _repositoryMock
            .Setup(r => r.GetDealStateCountsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DealStateCountsRow(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new List<DealStatusCountRow>()));

        _repositoryMock
            .Setup(r => r.GetDealSalesTotalsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DealSalesTotalsRow(0, 0m, 0m));

        _repositoryMock
            .Setup(r => r.GetPaymentStatusRowsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentStatusRow>());

        _repositoryMock
            .Setup(r => r.GetSucceededPaymentsByMethodAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentMethodAmountRow>());

        _repositoryMock
            .Setup(r => r.GetRefundStatusRowsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefundStatusRow>());

        _repositoryMock
            .Setup(r => r.GetRefundTypeRowsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RefundTypeRow>());

        _repositoryMock
            .Setup(r => r.GetTransferredRefundAmountAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // Act
        var result = await _service.GetDashboardAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Period.Should().NotBeNull();
        result.Selected.Should().NotBeNull();
        result.Overall.Should().NotBeNull();

        _output.WriteLine("[TCID01 PASSED] 200 OK - Platform dashboard snapshots loaded successfully.");
    }
}
