using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Api.Tests.UnitTests.Support;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Api.Tests.UnitTests;

[Trait("Category", "Unit")]
public class CancelOrderTests
{
    private readonly ITestOutputHelper _output;
    private readonly PaymentTestContext _context;
    private readonly CheckoutPaymentService _service;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<ICheckoutPaymentGateway> _gatewayMock;

    public CancelOrderTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new PaymentTestContext();
        _service = _context.Service;
        _orderRepoMock = _context.Repo;
        _gatewayMock = _context.Gateway;
    }

    // TCID01_CancelOrder_EligibleState_CancelsWithoutRefundingPaidMoney_Normal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.ReadyForPickup)]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_CancelOrder_EligibleState_CancelsWithoutRefundingPaidMoney_Normal(OrderStatus status)
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now, status); var order = _context.Order;

        // Act
        var result = await _service.CancelAsync(order.CustomerId, order.Id);

        // Assert
        result.Status.Should().Be("Cancelled");
        _context.Initial.Status.Should().Be(PaymentStatus.Succeeded);
        result.PaidAmount.Should().Be(5000m);
        order.Refunds.Should().BeEmpty();
        _orderRepoMock.Verify(r => r.ReleaseCancelledInventoryAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _context.Realtime.Verify(r => r.NotifyAsync(order.ShopId, order.Id, RealtimeEvents.OrderReasons.Cancelled, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: CancelOrder_EligibleState_CancelsWithoutRefundingPaidMoney (Normal).");
    }

    // TCID02_CancelOrder_UnpaidWithoutGateway_CancelsPendingPayment_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID02_CancelOrder_UnpaidWithoutGateway_CancelsPendingPayment_Normal()
    {
        // Arrange
        var order = _context.Order;

        // Act
        var result = await _service.CancelAsync(order.CustomerId, order.Id);

        // Assert
        result.Status.Should().Be("Cancelled");
        _context.Initial.Status.Should().Be(PaymentStatus.Cancelled);
        _gatewayMock.Verify(r => r.CancelAsync(It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID02 PASSED: CancelOrder_UnpaidWithoutGateway_CancelsPendingPayment (Normal).");
    }

    // TCID03_CancelOrder_TerminalState_ThrowsConflict_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Expired)]
    [InlineData(OrderStatus.NoShow)]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_CancelOrder_TerminalState_ThrowsConflict_Abnormal(OrderStatus status)
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now, status);

        // Act
        Func<Task> act = () => _service.CancelAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _orderRepoMock.Verify(r => r.ReleaseCancelledInventoryAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID03 PASSED: CancelOrder_TerminalState_ThrowsConflict (Abnormal).");
    }

    // TCID04_CancelOrder_ExactlyAtHardDeadline_ThrowsConflict_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID04_CancelOrder_ExactlyAtHardDeadline_ThrowsConflict_Boundary()
    {
        // Arrange
        _context.Order.HardDeadlineAt = _context.Clock.Now;

        // Act
        Func<Task> act = () => _service.CancelAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _output.WriteLine("TCID04 PASSED: CancelOrder_ExactlyAtHardDeadline_ThrowsConflict (Boundary).");
    }

    // TCID05_CancelOrder_OneTickBeforeDeadline_AllowsCancellation_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_CancelOrder_OneTickBeforeDeadline_AllowsCancellation_Boundary()
    {
        // Arrange
        _context.Order.HardDeadlineAt = _context.Clock.Now.AddTicks(1);

        // Act
        var result = await _service.CancelAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        result.Status.Should().Be("Cancelled");
        _output.WriteLine("TCID05 PASSED: CancelOrder_OneTickBeforeDeadline_AllowsCancellation (Boundary).");
    }

    // TCID06_CancelOrder_RepeatedRequest_DoesNotReleaseTwice_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID06_CancelOrder_RepeatedRequest_DoesNotReleaseTwice_Boundary()
    {
        // Arrange
        var order = _context.Order;
        await _service.CancelAsync(order.CustomerId, order.Id);

        // Act
        var result = await _service.CancelAsync(order.CustomerId, order.Id);

        // Assert
        result.Status.Should().Be("Cancelled");
        _orderRepoMock.Verify(r => r.ReleaseCancelledInventoryAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID06 PASSED: CancelOrder_RepeatedRequest_DoesNotReleaseTwice (Boundary).");
    }

    // TCID07_CancelOrder_AnotherCustomer_CannotAccessOrder_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID07_CancelOrder_AnotherCustomer_CannotAccessOrder_Abnormal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetOrderAsync(_context.Order.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = () => _service.CancelAsync(userId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _output.WriteLine("TCID07 PASSED: CancelOrder_AnotherCustomer_CannotAccessOrder (Abnormal).");
    }

    // TCID08_CancelOrder_ProviderClosureUncertain_KeepsReservation_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID08_CancelOrder_ProviderClosureUncertain_KeepsReservation_Abnormal()
    {
        // Arrange
        _context.Initial.GatewayCreationAttempted = true;
        _context.ProviderStatus = "PENDING";

        // Act
        var result = await _service.CancelAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        result.Status.Should().Be("AwaitingPayment");
        _orderRepoMock.Verify(r => r.ReleaseCancelledInventoryAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID08 PASSED: CancelOrder_ProviderClosureUncertain_KeepsReservation (Abnormal).");
    }

    // TCID09_CancelOrder_NoShowFlagOnReadyOrder_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID09_CancelOrder_NoShowFlagOnReadyOrder_Rejects_Abnormal()
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now, OrderStatus.ReadyForPickup); _context.Order.NoShow = true;

        // Act
        Func<Task> act = () => _service.CancelAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _orderRepoMock.Verify(r => r.ReleaseCancelledInventoryAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID09 PASSED: CancelOrder_NoShowFlagOnReadyOrder_Rejects (Abnormal).");
    }

    // TCID10_CancelOrder_PaymentWinsClosureRace_RecordsPaidMoneyWithoutRefund_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID10_CancelOrder_PaymentWinsClosureRace_RecordsPaidMoneyWithoutRefund_Abnormal()
    {
        // Arrange
        _context.Initial.GatewayCreationAttempted = true; _context.ProviderStatus = "PAID"; _context.ProviderPaid = 5000;

        // Act
        var result = await _service.CancelAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        result.Status.Should().Be("Cancelled");
        _context.Initial.Status.Should().Be(PaymentStatus.Succeeded);
        result.PaidAmount.Should().Be(5000m);
        _context.Order.Refunds.Should().BeEmpty();
        _output.WriteLine("TCID10 PASSED: CancelOrder_PaymentWinsClosureRace_RecordsPaidMoneyWithoutRefund (Abnormal).");
    }

    // TCID11_CancelOrder_RemainingQrCannotBeClosed_RejectsWithoutReleasingStock_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID11_CancelOrder_RemainingQrCannotBeClosed_RejectsWithoutReleasingStock_Abnormal()
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now, OrderStatus.ReadyForPickup);
        UnitTestData.Remaining(_context.Order, _context.Clock.Now);

        // Act
        Func<Task> act = () => _service.CancelAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        _context.Order.Status.Should().Be(OrderStatus.ReadyForPickup);
        _orderRepoMock.Verify(r => r.ReleaseCancelledInventoryAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID11 PASSED: CancelOrder_RemainingQrCannotBeClosed_RejectsWithoutReleasingStock (Abnormal).");
    }
}
