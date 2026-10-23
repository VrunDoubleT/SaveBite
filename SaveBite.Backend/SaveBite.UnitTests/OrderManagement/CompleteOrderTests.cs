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
public class CompleteOrderTests
{
    private readonly ITestOutputHelper _output;
    private readonly ShopOrderTestContext _context;
    private readonly ShopOrderService _service;
    private readonly Mock<IShopOrderRepository> _shopRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<IShopOrderEventService> _realtimeMock;

    public CompleteOrderTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new ShopOrderTestContext();
        _service = _context.Service;
        _shopRepoMock = _context.Shops;
        _orderRepoMock = _context.Orders;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_CompleteOrder_PaidRemaining_CompletesAndClearsBalance_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_CompleteOrder_PaidRemaining_CompletesAndClearsBalance_Normal()
    {
        // Arrange
        var order = _context.Order; order.Status = OrderStatus.ReadyForPickup;
        UnitTestData.Remaining(order, _context.Clock.Now, PaymentStatus.Succeeded);

        // Act
        var result = await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "complete");

        // Assert
        result.Order.Status.Should().Be("Completed");
        order.RemainingAmount.Should().Be(0);
        order.CompletedAt.Should().Be(_context.Clock.Now);
        _realtimeMock.Verify(r => r.NotifyAsync(order.ShopId, order.Id, RealtimeEvents.OrderReasons.Complete, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: CompleteOrder_PaidRemaining_CompletesAndClearsBalance (Normal).");
    }

    // TCID02_CompleteOrder_OnlineFullPayment_NeedsNoRemainingReceipt_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID02_CompleteOrder_OnlineFullPayment_NeedsNoRemainingReceipt_Normal()
    {
        // Arrange
        var order = _context.Order; order.Status = OrderStatus.ReadyForPickup; order.PaymentMethod = PaymentMethod.OnlinePayment;
        _context.Initial.PaymentType = PaymentType.Full; _context.Initial.Amount = 50000; _context.Initial.CollectedAmount = 50000;

        // Act
        var result = await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "complete");

        // Assert
        result.Order.Status.Should().Be("Completed");
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID02 PASSED: CompleteOrder_OnlineFullPayment_NeedsNoRemainingReceipt (Normal).");
    }

    // TCID03_CompleteOrder_CashReceipt_RecordsMethodAmountAndStaff_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID03_CompleteOrder_CashReceipt_RecordsMethodAmountAndStaff_Normal()
    {
        // Arrange
        _context.AsStaff(); var order = _context.Order; order.Status = OrderStatus.ReadyForPickup;

        // Act
        var result = await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "complete", cashReceived: true);

        // Assert
        result.Order.Status.Should().Be("Completed");
        var receipt = order.Payments.Single(p => p.PaymentType == PaymentType.Remaining);
        receipt.Method.Should().Be(PaymentMethod.Cash); receipt.Status.Should().Be(PaymentStatus.Succeeded);
        receipt.Amount.Should().Be(45000m); receipt.CollectedAmount.Should().Be(45000m);
        receipt.GatewayOrderCode.Should().BeNull();
        _shopRepoMock.Verify(r => r.AddPayment(It.Is<Payment>(p => p.Method == PaymentMethod.Cash && p.Amount == 45000)), Times.Once);
        _context.Gateway.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _shopRepoMock.Verify(r => r.AddActivity(It.Is<StaffActivityLog>(x => x.ActionBy == _context.ActorId && x.ActorRoleSnapshot == "Staff")), Times.Once);
        _output.WriteLine("TCID03 PASSED: CompleteOrder_CashReceipt_RecordsMethodAmountAndStaff (Normal).");
    }

    // TCID04_CompleteOrder_CashWithExistingUnpaidQr_ClosesAndReusesReceipt_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID04_CompleteOrder_CashWithExistingUnpaidQr_ClosesAndReusesReceipt_Normal()
    {
        // Arrange
        var order = _context.Order; order.Status = OrderStatus.ReadyForPickup;
        var qr = UnitTestData.Remaining(order, _context.Clock.Now);

        // Act
        await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "complete", cashReceived: true);

        // Assert
        order.Payments.Count(p => p.PaymentType == PaymentType.Remaining).Should().Be(1);
        qr.Method.Should().Be(PaymentMethod.Cash); qr.GatewayCheckoutUrl.Should().BeNull();
        _context.Gateway.Verify(r => r.CancelAsync(qr, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID04 PASSED: CompleteOrder_CashWithExistingUnpaidQr_ClosesAndReusesReceipt (Normal).");
    }

    // TCID05_CompleteOrder_UnpaidRemaining_RejectsWithoutCompletion_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID05_CompleteOrder_UnpaidRemaining_RejectsWithoutCompletion_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete");

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _context.Order.CompletedAt.Should().BeNull();
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID05 PASSED: CompleteOrder_UnpaidRemaining_RejectsWithoutCompletion (Abnormal).");
    }

    // TCID06_CompleteOrder_WrongState_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Expired)]
    [InlineData(OrderStatus.NoShow)]
    [InlineData(OrderStatus.Rejected)]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_CompleteOrder_WrongState_Rejects_Abnormal(OrderStatus state)
    {
        // Arrange
        _context.Order.Status = state;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete");

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _output.WriteLine("TCID06 PASSED: CompleteOrder_WrongState_Rejects (Abnormal).");
    }

    // TCID07_CompleteOrder_ExactlyAtHardDeadline_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID07_CompleteOrder_ExactlyAtHardDeadline_Rejects_Boundary()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup; _context.Order.HardDeadlineAt = _context.Clock.Now;
        UnitTestData.Remaining(_context.Order, _context.Clock.Now, PaymentStatus.Succeeded);

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _output.WriteLine("TCID07 PASSED: CompleteOrder_ExactlyAtHardDeadline_Rejects (Boundary).");
    }

    // TCID08_CompleteOrder_AfterSoftBeforeHard_RecordsLateArrival_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID08_CompleteOrder_AfterSoftBeforeHard_RecordsLateArrival_Boundary()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup; _context.Order.SoftDeadlineAt = _context.Clock.Now.AddTicks(-1);
        UnitTestData.Remaining(_context.Order, _context.Clock.Now, PaymentStatus.Succeeded);

        // Act
        await _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete");

        // Assert
        _context.Order.IsLateArrival.Should().BeTrue();
        _output.WriteLine("TCID08 PASSED: CompleteOrder_AfterSoftBeforeHard_RecordsLateArrival (Boundary).");
    }

    // TCID09_CompleteOrder_RepeatCashRequest_DoesNotCollectTwice_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID09_CompleteOrder_RepeatCashRequest_DoesNotCollectTwice_Boundary()
    {
        // Arrange
        var order = _context.Order; order.Status = OrderStatus.ReadyForPickup;
        await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "complete", cashReceived: true);

        // Act
        await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "complete", cashReceived: true);

        // Assert
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Once);
        order.StatusHistories.Count(x => x.ToStatus == OrderStatus.Completed).Should().Be(1);
        _output.WriteLine("TCID09 PASSED: CompleteOrder_RepeatCashRequest_DoesNotCollectTwice (Boundary).");
    }

    // TCID10_CompleteOrder_PartialOnlineCollection_BlocksCashRelabel_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID10_CompleteOrder_PartialOnlineCollection_BlocksCashRelabel_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        var qr = UnitTestData.Remaining(_context.Order, _context.Clock.Now); qr.CollectedAmount = 1000;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete", cashReceived: true);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        qr.Method.Should().Be(PaymentMethod.OnlinePayment);
        _output.WriteLine("TCID10 PASSED: CompleteOrder_PartialOnlineCollection_BlocksCashRelabel (Abnormal).");
    }

    // TCID11_CompleteOrder_OnlinePaidDuringCashClosure_BlocksSecondCollection_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID11_CompleteOrder_OnlinePaidDuringCashClosure_BlocksSecondCollection_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        var qr = UnitTestData.Remaining(_context.Order, _context.Clock.Now);
        _context.Gateway.Setup(r => r.CancelAsync(qr, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPayment("PAID", qr.GatewayOrderCode!.Value, 45000, 45000, "paid", null));

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete", cashReceived: true);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        qr.Method.Should().Be(PaymentMethod.OnlinePayment);
        _context.Order.Status.Should().Be(OrderStatus.ReadyForPickup);
        _output.WriteLine("TCID11 PASSED: CompleteOrder_OnlinePaidDuringCashClosure_BlocksSecondCollection (Abnormal).");
    }

    // TCID12_CompleteOrder_UnauthorizedOperator_RejectsBeforeMutation_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID12_CompleteOrder_UnauthorizedOperator_RejectsBeforeMutation_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        _shopRepoMock.Setup(r => r.RequireAccessAsync(_context.ActorId, _context.Order.ShopId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden());

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete", "Closed");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _realtimeMock.Verify(r => r.NotifyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID12 PASSED: CompleteOrder_UnauthorizedOperator_RejectsBeforeMutation (Abnormal).");
    }

    // TCID13_CompleteOrder_CashQrClosureUncertain_DoesNotRelabelReceipt_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID13_CompleteOrder_CashQrClosureUncertain_DoesNotRelabelReceipt_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        var qr = UnitTestData.Remaining(_context.Order, _context.Clock.Now);
        _context.Gateway.Setup(r => r.CancelAsync(qr, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPayment("PENDING", qr.GatewayOrderCode!.Value, 45000, 0, "pending", null));

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete", cashReceived: true);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        qr.Method.Should().Be(PaymentMethod.OnlinePayment);
        _context.Order.CompletedAt.Should().BeNull();
        _output.WriteLine("TCID13 PASSED: CompleteOrder_CashQrClosureUncertain_DoesNotRelabelReceipt (Abnormal).");
    }

    // TCID14_CompleteOrder_InitialFullOnlineOrder_CannotBeRelabeledCash_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID14_CompleteOrder_InitialFullOnlineOrder_CannotBeRelabeledCash_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup; _context.Order.PaymentMethod = PaymentMethod.OnlinePayment;
        _context.Initial.Amount = 50000;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete", cashReceived: true);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID14 PASSED: CompleteOrder_InitialFullOnlineOrder_CannotBeRelabeledCash (Abnormal).");
    }

    // TCID15_CompleteOrder_ReviewRequired_BlocksEvenFullyPaid_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID15_CompleteOrder_ReviewRequired_BlocksEvenFullyPaid_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup; _context.Order.PaymentReviewRequired = true;
        UnitTestData.Remaining(_context.Order, _context.Clock.Now, PaymentStatus.Succeeded);

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "complete");

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _context.Order.CompletedAt.Should().BeNull();
        _output.WriteLine("TCID15 PASSED: CompleteOrder_ReviewRequired_BlocksEvenFullyPaid (Abnormal).");
    }
}
