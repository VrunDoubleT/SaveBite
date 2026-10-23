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
public class RejectOrderTests
{
    private readonly ITestOutputHelper _output;
    private readonly ShopOrderTestContext _context;
    private readonly ShopOrderService _service;
    private readonly Mock<IShopOrderRepository> _shopRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<IShopOrderEventService> _realtimeMock;

    public RejectOrderTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new ShopOrderTestContext();
        _service = _context.Service;
        _shopRepoMock = _context.Shops;
        _orderRepoMock = _context.Orders;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_RejectOrder_PaidPending_ReleasesInventoryAndRecordsRefundOnly_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_RejectOrder_PaidPending_ReleasesInventoryAndRecordsRefundOnly_Normal()
    {
        // Arrange
        var order = _context.Order; order.ConfirmationExpirationJobId = "confirmation-job";

        // Act
        var result = await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "reject", " Shop is closed ");

        // Assert
        result.Order.Status.Should().Be("Rejected");
        order.CancelReason.Should().Be("Shop is closed");
        order.Refunds.Should().ContainSingle().Which.RequestedAmount.Should().Be(5000m);
        order.Refunds.Single().Status.Should().Be(RefundStatus.AwaitingCustomerDetails);
        _orderRepoMock.Verify(r => r.ReleaseCancelledInventoryAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _context.Confirmation.Verify(r => r.Delete("confirmation-job"), Times.Once);
        _realtimeMock.Verify(r => r.NotifyAsync(order.ShopId, order.Id, RealtimeEvents.OrderReasons.Reject, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: RejectOrder_PaidPending_ReleasesInventoryAndRecordsRefundOnly (Normal).");
    }

    // TCID02_RejectOrder_InvalidReason_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID02_RejectOrder_InvalidReason_Rejects_Boundary(int length)
    {
        // Arrange
        var reason = new string('x', length);

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "reject", reason);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _shopRepoMock.Verify(r => r.AddRefund(It.IsAny<Refund>()), Times.Never);
        _output.WriteLine("TCID02 PASSED: RejectOrder_InvalidReason_Rejects (Boundary).");
    }

    // TCID03_RejectOrder_ReasonLengthLimits_Accepted_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID03_RejectOrder_ReasonLengthLimits_Accepted_Boundary(int length)
    {
        // Arrange
        var reason = new string('x', length);

        // Act
        var result = await _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "reject", reason);

        // Assert
        result.Order.Status.Should().Be("Rejected");
        _output.WriteLine("TCID03 PASSED: RejectOrder_ReasonLengthLimits_Accepted (Boundary).");
    }

    // TCID04_RejectOrder_AlreadyConfirmedOrTerminal_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.ReadyForPickup)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID04_RejectOrder_AlreadyConfirmedOrTerminal_Rejects_Abnormal(OrderStatus status)
    {
        // Arrange
        _context.Order.Status = status;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "reject", "Cannot fulfill");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _orderRepoMock.Verify(r => r.ReleaseCancelledInventoryAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID04 PASSED: RejectOrder_AlreadyConfirmedOrTerminal_Rejects (Abnormal).");
    }

    // TCID05_RejectOrder_RepeatedRequest_DoesNotDuplicateRefund_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_RejectOrder_RepeatedRequest_DoesNotDuplicateRefund_Boundary()
    {
        // Arrange
        var order = _context.Order;
        await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "reject", "Closed");

        // Act
        await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "reject", "Closed");

        // Assert
        order.Refunds.Should().ContainSingle();
        _shopRepoMock.Verify(r => r.AddRefund(It.IsAny<Refund>()), Times.Once);
        _orderRepoMock.Verify(r => r.ReleaseCancelledInventoryAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID05 PASSED: RejectOrder_RepeatedRequest_DoesNotDuplicateRefund (Boundary).");
    }

    // TCID06_RejectOrder_NoCollectedMoney_DoesNotCreateRefund_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID06_RejectOrder_NoCollectedMoney_DoesNotCreateRefund_Boundary()
    {
        // Arrange
        _context.Initial.Status = PaymentStatus.Pending; _context.Initial.CollectedAmount = 0;

        // Act
        var result = await _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "reject", "Unavailable");

        // Assert
        result.Order.Status.Should().Be("Rejected");
        _shopRepoMock.Verify(r => r.AddRefund(It.IsAny<Refund>()), Times.Never);
        _output.WriteLine("TCID06 PASSED: RejectOrder_NoCollectedMoney_DoesNotCreateRefund (Boundary).");
    }

    // TCID07_RejectOrder_AtHardDeadline_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID07_RejectOrder_AtHardDeadline_Rejects_Boundary()
    {
        // Arrange
        _context.Order.HardDeadlineAt = _context.Clock.Now;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "reject", "Closed");

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _output.WriteLine("TCID07 PASSED: RejectOrder_AtHardDeadline_Rejects (Boundary).");
    }

    // TCID08_RejectOrder_UnauthorizedOperator_RejectsBeforeMutation_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID08_RejectOrder_UnauthorizedOperator_RejectsBeforeMutation_Abnormal()
    {
        // Arrange

        _shopRepoMock.Setup(r => r.RequireAccessAsync(_context.ActorId, _context.Order.ShopId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden());

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "reject", "Closed");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _realtimeMock.Verify(r => r.NotifyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID08 PASSED: RejectOrder_UnauthorizedOperator_RejectsBeforeMutation (Abnormal).");
    }
}
