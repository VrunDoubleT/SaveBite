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
/// Unit Tests for Feature: View Platform Fee Statistics
/// Service: PlatformAnalyticsService.GetPlatformFeeStatisticsAsync
/// Function Code: UT-129
/// Sheet Name: UT_ViewPlatformFeeStatistics
/// </summary>
public class ViewPlatformFeeStatisticsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformAnalyticsRepository> _repositoryMock = new();
    private readonly PlatformAnalyticsService _service;

    public ViewPlatformFeeStatisticsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformAnalyticsService(_repositoryMock.Object, TimeProvider.System);
    }

    // =========================================================================
    // TCID01: Active Fee Config & Accruals -> Returns Fee Statistics (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetPlatformFeeStatistics_ActiveConfigAndStatements_ReturnsFeeMetrics_Normal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest { Preset = "today" };
        var feeConfig = new PlatformFeeConfig
        {
            Id = Guid.NewGuid(),
            FeeRate = 0.08m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30)
        };

        var accrual = new FeeAccrualRow(FeeBearingOrders: 25, GrossFee: 500000m, Adjustments: -50000m);
        var statements = new FeeStatementSummaryRow(
            PaidInPeriod: 1200000m,
            OutstandingBalance: 300000m,
            StatusBreakdown: new List<StatementStatusRow>
            {
                new(PlatformFeeStatementStatus.Paid, 4, 1200000m),
                new(PlatformFeeStatementStatus.Issued, 1, 300000m)
            },
            Recent: new List<FeeStatementRow>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Tiệm Bánh A", 2026, 10, PlatformFeeStatementStatus.Paid, 300000m, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow)
            }
        );

        _repositoryMock
            .Setup(r => r.GetCurrentFeeConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(feeConfig);

        _repositoryMock
            .Setup(r => r.GetFeeAccrualAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accrual);

        _repositoryMock
            .Setup(r => r.GetFeeStatementSummaryAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statements);

        // Act
        var result = await _service.GetPlatformFeeStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CurrentFeeRatePercent.Should().Be(8.0m);
        result.GrossAccruedFeeAmount.Should().Be(500000m);
        result.RefundFeeAdjustmentAmount.Should().Be(-50000m);
        result.NetAccruedFeeAmount.Should().Be(450000m); // 500k - 50k
        result.TotalPaidStatementAmount.Should().Be(1200000m);
        result.TotalOutstandingStatementAmount.Should().Be(300000m);
        result.TotalFeeBearingOrders.Should().Be(25);
        result.RecentStatements.Should().HaveCount(1);
        result.RecentStatements[0].ShopName.Should().Be("Tiệm Bánh A");

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Fee Rate: {result.CurrentFeeRatePercent}%, Net Fee: {result.NetAccruedFeeAmount:N0} VND.");
    }

    // =========================================================================
    // TCID02: No Config Configured -> Returns Null Fee Rate (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetPlatformFeeStatistics_NoConfigFound_ReturnsNullFeeRate_Boundary()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest { Preset = "today" };
        var accrual = new FeeAccrualRow(0, 0m, 0m);
        var statements = new FeeStatementSummaryRow(0m, 0m, new List<StatementStatusRow>(), new List<FeeStatementRow>());

        _repositoryMock
            .Setup(r => r.GetCurrentFeeConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeeConfig?)null);

        _repositoryMock
            .Setup(r => r.GetFeeAccrualAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accrual);

        _repositoryMock
            .Setup(r => r.GetFeeStatementSummaryAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statements);

        // Act
        var result = await _service.GetPlatformFeeStatisticsAsync(request);

        // Assert
        result.CurrentFeeRatePercent.Should().BeNull();
        result.GrossAccruedFeeAmount.Should().Be(0m);
        result.TotalFeeBearingOrders.Should().Be(0);

        _output.WriteLine("[TCID02 PASSED] When fee config is missing, fee rate returns null as expected without throwing.");
    }
}
