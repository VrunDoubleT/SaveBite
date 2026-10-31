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
/// Unit Tests for Feature: View Payment and Refund Statistics
/// Service: PlatformAnalyticsService.GetPaymentAndRefundStatisticsAsync
/// Function Code: UT-134
/// Sheet Name: UT_ViewPaymentAndRefundStatistics
/// </summary>
public class ViewPaymentAndRefundStatisticsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformAnalyticsRepository> _repositoryMock = new();
    private readonly PlatformAnalyticsService _service;

    public ViewPaymentAndRefundStatisticsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformAnalyticsService(_repositoryMock.Object, TimeProvider.System);
    }

    // =========================================================================
    // TCID01: Payments & Refunds Present -> Returns Gateway Reliability & Refund Metrics (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetPaymentAndRefundStatistics_ValidRequest_ReturnsGatewayAndRefundMetrics_Normal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest { Preset = "today" };

        var paymentStatusRows = new List<PaymentStatusRow>
        {
            new(PaymentStatus.Succeeded, 10, 1000000m),
            new(PaymentStatus.Pending, 2, 200000m),
            new(PaymentStatus.Failed, 1, 100000m)
        };

        var methodRows = new List<PaymentMethodAmountRow>
        {
            new(PaymentMethod.OnlinePayment, 7, 700000m),
            new(PaymentMethod.PayAtShop, 3, 300000m)
        };

        var refundStatusRows = new List<RefundStatusRow>
        {
            new(RefundStatus.Completed, 2, 150000m),
            new(RefundStatus.PendingVerification, 1, 50000m),
            new(RefundStatus.Rejected, 1, 50000m)
        };

        var refundTypeRows = new List<RefundTypeRow>
        {
            new(RefundType.CustomerRequest, 3, 200000m),
            new(RefundType.ShopCancellation, 1, 50000m)
        };

        _repositoryMock
            .Setup(r => r.GetPaymentStatusRowsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentStatusRows);

        _repositoryMock
            .Setup(r => r.GetSucceededPaymentsByMethodAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(methodRows);

        _repositoryMock
            .Setup(r => r.GetRefundStatusRowsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundStatusRows);

        _repositoryMock
            .Setup(r => r.GetRefundTypeRowsAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundTypeRows);

        _repositoryMock
            .Setup(r => r.GetTransferredRefundAmountAsync(It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(150000m);

        // Act
        var result = await _service.GetPaymentAndRefundStatisticsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalPaymentTransactions.Should().Be(13);
        result.SucceededPaymentsCount.Should().Be(10);
        result.PendingPaymentsCount.Should().Be(2);
        result.FailedOrCancelledPaymentsCount.Should().Be(1);
        result.TotalCollectedPaymentAmount.Should().Be(1000000m);

        result.TotalRefundRequests.Should().Be(4);
        result.CompletedOrTransferredRefundsCount.Should().Be(2);
        result.InProgressRefundsCount.Should().Be(1);
        result.RejectedOrWithdrawnRefundsCount.Should().Be(1);
        result.TotalRequestedRefundAmount.Should().Be(250000m);
        result.TotalTransferredRefundAmount.Should().Be(150000m);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - 10 succeeded payments (1M VND), 150k VND transferred in refunds.");
    }
}
