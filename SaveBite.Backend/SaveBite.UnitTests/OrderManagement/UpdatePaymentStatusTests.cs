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
public class UpdatePaymentStatusTests
{
    private readonly ITestOutputHelper _output;
    private readonly PaymentTestContext _context;
    private readonly CheckoutPaymentService _service;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<ICheckoutPaymentGateway> _gatewayMock;

    public UpdatePaymentStatusTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new PaymentTestContext();
        _service = _context.Service;
        _orderRepoMock = _context.Repo;
        _gatewayMock = _context.Gateway;
    }

    // TCID01_UpdatePaymentStatus_VerifiedWebhook_SubmitsOrderAndSettlesReservation_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_UpdatePaymentStatus_VerifiedWebhook_SubmitsOrderAndSettlesReservation_Normal()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.FindGatewayOrderAsync(_context.Initial.GatewayOrderCode!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(order.Id);
        _gatewayMock.Setup(r => r.VerifyWebhook(It.IsAny<JsonElement>(), order.Shop.PaymentConfig!)).Returns(true);
        var body = JsonSerializer.SerializeToElement(new { data = new { orderCode = _context.Initial.GatewayOrderCode!.Value }, signature = "test-signature" });
        _context.Initial.GatewayCreationAttempted = true;
        _context.ProviderStatus = "PAID"; _context.ProviderPaid = 5000;
        order.PaymentExpirationJobId = "expiry-job";

        // Act
        await _service.WebhookAsync(body);

        // Assert
        order.Status.Should().Be(OrderStatus.Pending);
        _context.Initial.Status.Should().Be(PaymentStatus.Succeeded);
        _context.Initial.CollectedAmount.Should().Be(5000m);
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(order, true, It.IsAny<CancellationToken>()), Times.Once);
        _context.Events.Verify(r => r.QueuePendingAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _context.Realtime.Verify(r => r.NotifyAsync(order.ShopId, order.Id, RealtimeEvents.OrderReasons.Pending, It.IsAny<CancellationToken>()), Times.Once);
        _context.Scheduler.Verify(r => r.Delete("expiry-job"), Times.Once);
        _output.WriteLine("TCID01 PASSED: UpdatePaymentStatus_VerifiedWebhook_SubmitsOrderAndSettlesReservation (Normal).");
    }

    // TCID02_UpdatePaymentStatus_DuplicateWebhook_DoesNotSettleOrSubmitTwice_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID02_UpdatePaymentStatus_DuplicateWebhook_DoesNotSettleOrSubmitTwice_Boundary()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.FindGatewayOrderAsync(_context.Initial.GatewayOrderCode!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(order.Id);
        _gatewayMock.Setup(r => r.VerifyWebhook(It.IsAny<JsonElement>(), order.Shop.PaymentConfig!)).Returns(true);
        var body = JsonSerializer.SerializeToElement(new { data = new { orderCode = _context.Initial.GatewayOrderCode!.Value }, signature = "test-signature" });
        _context.Initial.GatewayCreationAttempted = true; _context.ProviderStatus = "PAID"; _context.ProviderPaid = 5000;
        await _service.WebhookAsync(body);

        // Act
        await _service.WebhookAsync(body);

        // Assert
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(order, true, It.IsAny<CancellationToken>()), Times.Once);
        _context.Events.Verify(r => r.QueuePendingAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _context.Realtime.Verify(r => r.NotifyAsync(order.ShopId, order.Id, RealtimeEvents.OrderReasons.Pending, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID02 PASSED: UpdatePaymentStatus_DuplicateWebhook_DoesNotSettleOrSubmitTwice (Boundary).");
    }

    // TCID03_UpdatePaymentStatus_InvalidSignature_ThrowsForbiddenWithoutProviderRead_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_UpdatePaymentStatus_InvalidSignature_ThrowsForbiddenWithoutProviderRead_Abnormal()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.FindGatewayOrderAsync(_context.Initial.GatewayOrderCode!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(order.Id);
        _gatewayMock.Setup(r => r.VerifyWebhook(It.IsAny<JsonElement>(), order.Shop.PaymentConfig!)).Returns(true);
        var body = JsonSerializer.SerializeToElement(new { data = new { orderCode = _context.Initial.GatewayOrderCode!.Value }, signature = "test-signature" });
        _gatewayMock.Setup(r => r.VerifyWebhook(It.IsAny<JsonElement>(), order.Shop.PaymentConfig!)).Returns(false);

        // Act
        Func<Task> act = () => _service.WebhookAsync(body);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _gatewayMock.Verify(r => r.GetAsync(It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID03 PASSED: UpdatePaymentStatus_InvalidSignature_ThrowsForbiddenWithoutProviderRead (Abnormal).");
    }

    // TCID04_UpdatePaymentStatus_MalformedWebhook_ThrowsBadRequest_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"data\":{\"orderCode\":\"123\"}}")]
    [InlineData("{\"data\":{\"orderCode\":1.5}}")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID04_UpdatePaymentStatus_MalformedWebhook_ThrowsBadRequest_Abnormal(string json)
    {
        // Arrange
        var body = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        Func<Task> act = () => _service.WebhookAsync(body);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _orderRepoMock.Verify(r => r.FindGatewayOrderAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID04 PASSED: UpdatePaymentStatus_MalformedWebhook_ThrowsBadRequest (Abnormal).");
    }

    // TCID05_UpdatePaymentStatus_UnknownGatewayCode_IsIgnored_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_UpdatePaymentStatus_UnknownGatewayCode_IsIgnored_Boundary()
    {
        // Arrange
        var body = JsonSerializer.SerializeToElement(new { data = new { orderCode = 999L } });

        // Act
        await _service.WebhookAsync(body);

        // Assert
        _orderRepoMock.Verify(r => r.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _context.Initial.Status.Should().Be(PaymentStatus.Pending);
        _output.WriteLine("TCID05 PASSED: UpdatePaymentStatus_UnknownGatewayCode_IsIgnored (Boundary).");
    }

    // TCID06_UpdatePaymentStatus_WebhookClaimsSuccess_StillRequiresProviderVerification_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_UpdatePaymentStatus_WebhookClaimsSuccess_StillRequiresProviderVerification_Abnormal()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.FindGatewayOrderAsync(_context.Initial.GatewayOrderCode!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(order.Id);
        _gatewayMock.Setup(r => r.VerifyWebhook(It.IsAny<JsonElement>(), order.Shop.PaymentConfig!)).Returns(true);
        var body = JsonSerializer.SerializeToElement(new { data = new { orderCode = _context.Initial.GatewayOrderCode!.Value }, signature = "test-signature" });
        _context.Initial.GatewayCreationAttempted = true;
        body = JsonSerializer.SerializeToElement(new { success = true, data = new { orderCode = _context.Initial.GatewayOrderCode, amount = 5000, status = "PAID" } });

        // Act
        await _service.WebhookAsync(body);

        // Assert
        _context.Initial.Status.Should().Be(PaymentStatus.Pending);
        _gatewayMock.Verify(r => r.GetAsync(_context.Initial, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(It.IsAny<Order>(), true, It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID06 PASSED: UpdatePaymentStatus_WebhookClaimsSuccess_StillRequiresProviderVerification (Abnormal).");
    }

    // TCID07_UpdatePaymentStatus_OneUnitUnderRequiredAmount_DoesNotSubmit_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID07_UpdatePaymentStatus_OneUnitUnderRequiredAmount_DoesNotSubmit_Boundary()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.FindGatewayOrderAsync(_context.Initial.GatewayOrderCode!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(order.Id);
        _gatewayMock.Setup(r => r.VerifyWebhook(It.IsAny<JsonElement>(), order.Shop.PaymentConfig!)).Returns(true);
        var body = JsonSerializer.SerializeToElement(new { data = new { orderCode = _context.Initial.GatewayOrderCode!.Value }, signature = "test-signature" });
        _context.Initial.GatewayCreationAttempted = true; _context.ProviderStatus = "PAID"; _context.ProviderPaid = 4999;

        // Act
        await _service.WebhookAsync(body);

        // Assert
        _context.Initial.Status.Should().Be(PaymentStatus.Pending);
        _context.Initial.CollectedAmount.Should().Be(4999m);
        order.Status.Should().Be(OrderStatus.AwaitingPayment);
        _context.Events.Verify(r => r.QueuePendingAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID07 PASSED: UpdatePaymentStatus_OneUnitUnderRequiredAmount_DoesNotSubmit (Boundary).");
    }

    // TCID08_UpdatePaymentStatus_Overpayment_RecordsMoneyAndFlagsReview_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID08_UpdatePaymentStatus_Overpayment_RecordsMoneyAndFlagsReview_Boundary()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.FindGatewayOrderAsync(_context.Initial.GatewayOrderCode!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(order.Id);
        _gatewayMock.Setup(r => r.VerifyWebhook(It.IsAny<JsonElement>(), order.Shop.PaymentConfig!)).Returns(true);
        var body = JsonSerializer.SerializeToElement(new { data = new { orderCode = _context.Initial.GatewayOrderCode!.Value }, signature = "test-signature" });
        _context.Initial.GatewayCreationAttempted = true; _context.ProviderStatus = "PAID"; _context.ProviderPaid = 5001;

        // Act
        await _service.WebhookAsync(body);

        // Assert
        order.PaymentReviewRequired.Should().BeTrue();
        _context.Initial.Status.Should().Be(PaymentStatus.Succeeded);
        _context.Initial.CollectedAmount.Should().Be(5001m);
        _output.WriteLine("TCID08 PASSED: UpdatePaymentStatus_Overpayment_RecordsMoneyAndFlagsReview (Boundary).");
    }

    // TCID09_UpdatePaymentStatus_PaidAfterHardDeadline_DoesNotReviveOrder_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID09_UpdatePaymentStatus_PaidAfterHardDeadline_DoesNotReviveOrder_Abnormal()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.FindGatewayOrderAsync(_context.Initial.GatewayOrderCode!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(order.Id);
        _gatewayMock.Setup(r => r.VerifyWebhook(It.IsAny<JsonElement>(), order.Shop.PaymentConfig!)).Returns(true);
        var body = JsonSerializer.SerializeToElement(new { data = new { orderCode = _context.Initial.GatewayOrderCode!.Value }, signature = "test-signature" });
        _context.Initial.GatewayCreationAttempted = true; _context.ProviderStatus = "PAID"; _context.ProviderPaid = 5000;
        order.HardDeadlineAt = _context.Clock.Now;

        // Act
        await _service.WebhookAsync(body);

        // Assert
        order.Status.Should().Be(OrderStatus.Expired);
        order.PaymentReviewRequired.Should().BeTrue();
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(order, false, It.IsAny<CancellationToken>()), Times.Once);
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(order, true, It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID09 PASSED: UpdatePaymentStatus_PaidAfterHardDeadline_DoesNotReviveOrder (Abnormal).");
    }

    // TCID10_UpdatePaymentStatus_AtUnpaidExpiry_ReleasesReservationOnce_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID10_UpdatePaymentStatus_AtUnpaidExpiry_ReleasesReservationOnce_Boundary()
    {
        // Arrange
        var order = _context.Order; order.PaymentExpiresAt = _context.Clock.Now;

        // Act
        await _service.ExpireAsync(order.Id);
        await _service.ExpireAsync(order.Id);

        // Assert
        order.Status.Should().Be(OrderStatus.Expired);
        _context.Initial.Status.Should().Be(PaymentStatus.Expired);
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(order, false, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID10 PASSED: UpdatePaymentStatus_AtUnpaidExpiry_ReleasesReservationOnce (Boundary).");
    }

    // TCID11_UpdatePaymentStatus_ExpiryBeforeDeadline_ThrowsForJobRetry_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID11_UpdatePaymentStatus_ExpiryBeforeDeadline_ThrowsForJobRetry_Boundary()
    {
        // Arrange
        var orderId = _context.Order.Id;

        // Act
        Func<Task> act = () => _service.ExpireAsync(orderId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(It.IsAny<Order>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID11 PASSED: UpdatePaymentStatus_ExpiryBeforeDeadline_ThrowsForJobRetry (Boundary).");
    }

    // TCID12_UpdatePaymentStatus_ExpiryProviderUncertain_KeepsStockAndRequestsRetry_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID12_UpdatePaymentStatus_ExpiryProviderUncertain_KeepsStockAndRequestsRetry_Abnormal()
    {
        // Arrange
        _context.Order.PaymentExpiresAt = _context.Clock.Now; _context.Initial.GatewayCreationAttempted = true;

        // Act
        Func<Task> act = () => _service.ExpireAsync(_context.Order.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _context.Order.InventoryReservationActive.Should().BeTrue();
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(It.IsAny<Order>(), false, It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID12 PASSED: UpdatePaymentStatus_ExpiryProviderUncertain_KeepsStockAndRequestsRetry (Abnormal).");
    }

    // TCID13_UpdatePaymentStatus_RemainingPaymentSuccess_NotifiesWithoutChangingPreparation_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID13_UpdatePaymentStatus_RemainingPaymentSuccess_NotifiesWithoutChangingPreparation_Normal()
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now, OrderStatus.ReadyForPickup);
        var remaining = UnitTestData.Remaining(_context.Order, _context.Clock.Now);
        _context.ProviderStatus = "PAID"; _context.ProviderPaid = 45000;

        // Act
        var result = await _service.ReconcileOrderAsync(_context.Order.Id);

        // Assert
        result.Status.Should().Be("ReadyForPickup"); result.OutstandingAmount.Should().Be(0);
        remaining.Status.Should().Be(PaymentStatus.Succeeded);
        _context.Realtime.Verify(r => r.NotifyAsync(_context.Order.ShopId, _context.Order.Id, RealtimeEvents.OrderReasons.PaymentSucceeded, It.IsAny<CancellationToken>()), Times.Once);
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(It.IsAny<Order>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID13 PASSED: UpdatePaymentStatus_RemainingPaymentSuccess_NotifiesWithoutChangingPreparation (Normal).");
    }

    // TCID14_UpdatePaymentStatus_LateSuccessOnOldQr_FlagsReviewAndClosesNewAttempt_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID14_UpdatePaymentStatus_LateSuccessOnOldQr_FlagsReviewAndClosesNewAttempt_Abnormal()
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now, OrderStatus.ReadyForPickup);
        var old = UnitTestData.Remaining(_context.Order, _context.Clock.Now.AddMinutes(-1), PaymentStatus.Expired); old.GatewayOrderCode = 111;
        var current = UnitTestData.Remaining(_context.Order, _context.Clock.Now); current.GatewayOrderCode = 222;
        _gatewayMock.Setup(r => r.GetAsync(old, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPayment("PAID", 111, 45000, 45000, "old", null));
        _gatewayMock.Setup(r => r.CancelAsync(current, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayPayment("CANCELLED", 222, 45000, 0, "new", null));

        // Act
        var result = await _service.ReconcileOrderAsync(_context.Order.Id);

        // Assert
        old.Status.Should().Be(PaymentStatus.Succeeded);
        current.Status.Should().Be(PaymentStatus.Cancelled);
        result.PaymentReviewRequired.Should().BeTrue();
        result.PaidAmount.Should().Be(50000m);
        _output.WriteLine("TCID14 PASSED: UpdatePaymentStatus_LateSuccessOnOldQr_FlagsReviewAndClosesNewAttempt (Abnormal).");
    }
}
