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
public class GenerateRemainingPaymentQrTests
{
    private readonly ITestOutputHelper _output;
    private readonly ShopOrderTestContext _context;
    private readonly ShopOrderService _service;
    private readonly Mock<IShopOrderRepository> _shopRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<IShopOrderEventService> _realtimeMock;

    public GenerateRemainingPaymentQrTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new ShopOrderTestContext();
        _service = _context.Service;
        _shopRepoMock = _context.Shops;
        _orderRepoMock = _context.Orders;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_GenerateRemainingPaymentQr_EligibleOrder_CreatesRecordedBalance_Normal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.ReadyForPickup)]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_GenerateRemainingPaymentQr_EligibleOrder_CreatesRecordedBalance_Normal(OrderStatus state)
    {
        // Arrange
        _context.Order.Status = state; var order = _context.Order;

        // Act
        var result = await _service.RemainingPaymentAsync(_context.ActorId, order.ShopId, order.Id);

        // Assert
        var payment = result.Payments.Single(x => x.Type == "Remaining");
        payment.Amount.Should().Be(45000m); payment.Status.Should().Be("Pending");
        payment.CheckoutUrl.Should().Be("https://pay.payos.vn/web/remaining");
        _shopRepoMock.Verify(r => r.AddPayment(It.Is<Payment>(p => p.Amount == 45000 && p.PaymentType == PaymentType.Remaining)), Times.Once);
        _context.Payments.Verify(r => r.ReconcileOrderAsync(order.Id, true, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: GenerateRemainingPaymentQr_EligibleOrder_CreatesRecordedBalance (Normal).");
    }

    // TCID02_GenerateRemainingPaymentQr_ExistingPending_ReusesSameIdentity_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID02_GenerateRemainingPaymentQr_ExistingPending_ReusesSameIdentity_Boundary()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        var previous = UnitTestData.Remaining(_context.Order, _context.Clock.Now);

        // Act
        var result = await _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        result.Payments.Single(x => x.Type == "Remaining").Id.Should().Be(previous.Id);
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID02 PASSED: GenerateRemainingPaymentQr_ExistingPending_ReusesSameIdentity (Boundary).");
    }

    // TCID03_GenerateRemainingPaymentQr_ClosedZeroCollected_CreatesNewAttempt_Normal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(PaymentStatus.Cancelled)]
    [InlineData(PaymentStatus.Expired)]
    [InlineData(PaymentStatus.Failed)]
    [Trait("CaseType", "Normal")]
    public async Task TCID03_GenerateRemainingPaymentQr_ClosedZeroCollected_CreatesNewAttempt_Normal(PaymentStatus status)
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        var old = UnitTestData.Remaining(_context.Order, _context.Clock.Now.AddMinutes(-1), status);

        // Act
        var result = await _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        result.Payments.Count(x => x.Type == "Remaining").Should().Be(2);
        var active = _context.Order.Payments.Single(x => x.PaymentType == PaymentType.Remaining && x.Status == PaymentStatus.Pending);
        active.Id.Should().NotBe(old.Id); active.GatewayOrderCode.Should().NotBe(old.GatewayOrderCode);
        old.Status.Should().Be(status);
        _context.Gateway.Verify(r => r.GetAsync(old, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID03 PASSED: GenerateRemainingPaymentQr_ClosedZeroCollected_CreatesNewAttempt (Normal).");
    }

    // TCID04_GenerateRemainingPaymentQr_PartialCollection_BlocksRegeneration_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID04_GenerateRemainingPaymentQr_PartialCollection_BlocksRegeneration_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        UnitTestData.Remaining(_context.Order, _context.Clock.Now, PaymentStatus.Expired).CollectedAmount = 1000;

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID04 PASSED: GenerateRemainingPaymentQr_PartialCollection_BlocksRegeneration (Abnormal).");
    }

    // TCID05_GenerateRemainingPaymentQr_PreviousProviderStateUncertain_BlocksNewAttempt_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID05_GenerateRemainingPaymentQr_PreviousProviderStateUncertain_BlocksNewAttempt_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        var old = UnitTestData.Remaining(_context.Order, _context.Clock.Now, PaymentStatus.Expired);
        _context.Gateway.Setup(r => r.GetAsync(old, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPayment("PENDING", old.GatewayOrderCode!.Value, 45000, 0, null, null));

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID05 PASSED: GenerateRemainingPaymentQr_PreviousProviderStateUncertain_BlocksNewAttempt (Abnormal).");
    }

    // TCID06_GenerateRemainingPaymentQr_AlreadyPaid_DoesNotCreateAgain_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID06_GenerateRemainingPaymentQr_AlreadyPaid_DoesNotCreateAgain_Boundary()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        UnitTestData.Remaining(_context.Order, _context.Clock.Now, PaymentStatus.Succeeded);

        // Act
        var result = await _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        result.Order.OutstandingAmount.Should().Be(0);
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID06 PASSED: GenerateRemainingPaymentQr_AlreadyPaid_DoesNotCreateAgain (Boundary).");
    }

    // TCID07_GenerateRemainingPaymentQr_IneligibleState_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.NoShow)]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID07_GenerateRemainingPaymentQr_IneligibleState_Rejects_Abnormal(OrderStatus state)
    {
        // Arrange
        _context.Order.Status = state;

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID07 PASSED: GenerateRemainingPaymentQr_IneligibleState_Rejects (Abnormal).");
    }

    // TCID08_GenerateRemainingPaymentQr_AtPickupDeadline_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID08_GenerateRemainingPaymentQr_AtPickupDeadline_Rejects_Boundary()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup; _context.Order.HardDeadlineAt = _context.Clock.Now;

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _output.WriteLine("TCID08 PASSED: GenerateRemainingPaymentQr_AtPickupDeadline_Rejects (Boundary).");
    }

    // TCID09_GenerateRemainingPaymentQr_OnlineFullPayment_IsIneligible_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID09_GenerateRemainingPaymentQr_OnlineFullPayment_IsIneligible_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup; _context.Order.PaymentMethod = PaymentMethod.OnlinePayment;

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _output.WriteLine("TCID09 PASSED: GenerateRemainingPaymentQr_OnlineFullPayment_IsIneligible (Abnormal).");
    }

    // TCID10_GenerateRemainingPaymentQr_ProviderConfigurationMissing_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID10_GenerateRemainingPaymentQr_ProviderConfigurationMissing_Rejects_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        _context.Gateway.Setup(r => r.IsConfigured(It.IsAny<ShopPaymentConfig>())).Returns(false);

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        _output.WriteLine("TCID10 PASSED: GenerateRemainingPaymentQr_ProviderConfigurationMissing_Rejects (Abnormal).");
    }

    // TCID11_GenerateRemainingPaymentQr_CreationResponseMissingLink_ReturnsRetryableError_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID11_GenerateRemainingPaymentQr_CreationResponseMissingLink_ReturnsRetryableError_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        _context.Payments.Setup(r => r.ReconcileOrderAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(new CheckoutOrderResponse());

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        _context.Order.Payments.Count(x => x.PaymentType == PaymentType.Remaining).Should().Be(1);
        _output.WriteLine("TCID11 PASSED: GenerateRemainingPaymentQr_CreationResponseMissingLink_ReturnsRetryableError (Abnormal).");
    }

    // TCID12_GenerateRemainingPaymentQr_UnauthorizedOperator_RejectsBeforeCreation_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID12_GenerateRemainingPaymentQr_UnauthorizedOperator_RejectsBeforeCreation_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        _shopRepoMock.Setup(r => r.RequireAccessAsync(_context.ActorId, _context.Order.ShopId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden());

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID12 PASSED: GenerateRemainingPaymentQr_UnauthorizedOperator_RejectsBeforeCreation (Abnormal).");
    }

    // TCID13_GenerateRemainingPaymentQr_OldAttemptWasPaid_RemainsBlocked_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID13_GenerateRemainingPaymentQr_OldAttemptWasPaid_RemainsBlocked_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup;
        var old = UnitTestData.Remaining(_context.Order, _context.Clock.Now, PaymentStatus.Expired);
        _context.Gateway.Setup(r => r.GetAsync(old, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPayment("PAID", old.GatewayOrderCode!.Value, 45000, 45000, "paid", null));

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID13 PASSED: GenerateRemainingPaymentQr_OldAttemptWasPaid_RemainsBlocked (Abnormal).");
    }

    // TCID14_GenerateRemainingPaymentQr_PaymentReviewRequired_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID14_GenerateRemainingPaymentQr_PaymentReviewRequired_Rejects_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.ReadyForPickup; _context.Order.PaymentReviewRequired = true;

        // Act
        Func<Task> act = () => _service.RemainingPaymentAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _shopRepoMock.Verify(r => r.AddPayment(It.IsAny<Payment>()), Times.Never);
        _output.WriteLine("TCID14 PASSED: GenerateRemainingPaymentQr_PaymentReviewRequired_Rejects (Abnormal).");
    }
}
