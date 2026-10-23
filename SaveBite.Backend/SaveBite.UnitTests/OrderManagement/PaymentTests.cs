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
public class PaymentTests
{
    private readonly ITestOutputHelper _output;
    private readonly PaymentTestContext _context;
    private readonly CheckoutPaymentService _service;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<ICheckoutPaymentGateway> _gatewayMock;

    public PaymentTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new PaymentTestContext();
        _service = _context.Service;
        _orderRepoMock = _context.Repo;
        _gatewayMock = _context.Gateway;
    }

    // TCID01_Payment_Start_PersistsIntentThenReturnsLink_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_Payment_Start_PersistsIntentThenReturnsLink_Normal()
    {
        // Arrange
        var order = _context.Order;

        // Act
        var result = await _service.StartAsync(order.CustomerId, order.Id);

        // Assert
        result.CheckoutUrl.Should().Be("https://pay.payos.vn/web/test");
        _context.Initial.GatewayCreationAttempted.Should().BeTrue();
        _gatewayMock.Verify(r => r.CreateAsync(order, _context.Initial, order.Shop.PaymentConfig!, It.IsAny<CancellationToken>()), Times.Once);
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _output.WriteLine("TCID01 PASSED: Payment_Start_PersistsIntentThenReturnsLink (Normal).");
    }

    // TCID02_Payment_ExistingLink_ReusesProviderRequest_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID02_Payment_ExistingLink_ReusesProviderRequest_Boundary()
    {
        // Arrange
        var order = _context.Order; _context.Initial.GatewayCreationAttempted = true;
        _context.Initial.GatewayCheckoutUrl = "https://pay.payos.vn/web/test";

        // Act
        await _service.StartAsync(order.CustomerId, order.Id);

        // Assert
        _gatewayMock.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _gatewayMock.Verify(r => r.GetAsync(_context.Initial, It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID02 PASSED: Payment_ExistingLink_ReusesProviderRequest (Boundary).");
    }

    // TCID03_Payment_GatewayUnavailable_KeepsOrderUnpaid_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_Payment_GatewayUnavailable_KeepsOrderUnpaid_Abnormal()
    {
        // Arrange
        _gatewayMock.Setup(r => r.IsConfigured(It.IsAny<ShopPaymentConfig>())).Returns(false);

        // Act
        var result = await _service.StartAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        result.Status.Should().Be("AwaitingPayment");
        result.PaymentStatus.Should().Be("Pending");
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(It.IsAny<Order>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID03 PASSED: Payment_GatewayUnavailable_KeepsOrderUnpaid (Abnormal).");
    }

    // TCID04_Payment_ProviderTimeout_DoesNotReleaseReservation_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID04_Payment_ProviderTimeout_DoesNotReleaseReservation_Abnormal()
    {
        // Arrange
        _gatewayMock.Setup(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>())).ThrowsAsync(AppException.ServiceUnavailable("Provider unavailable."));
        _gatewayMock.Setup(r => r.GetAsync(It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>())).ThrowsAsync(AppException.ServiceUnavailable("Provider unavailable."));

        // Act
        var result = await _service.StartAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        result.PaymentMessage.Should().Be("Provider unavailable.");
        _context.Order.InventoryReservationActive.Should().BeTrue();
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(It.IsAny<Order>(), false, It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID04 PASSED: Payment_ProviderTimeout_DoesNotReleaseReservation (Abnormal).");
    }

    // TCID05_Payment_AlreadySucceeded_DoesNotCreateAnotherLink_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_Payment_AlreadySucceeded_DoesNotCreateAnotherLink_Boundary()
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now);

        // Act
        var result = await _service.StartAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        result.PaymentStatus.Should().Be("Succeeded");
        _gatewayMock.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID05 PASSED: Payment_AlreadySucceeded_DoesNotCreateAnotherLink (Boundary).");
    }

    // TCID06_Payment_ZeroDeposit_DoesNotContactProvider_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID06_Payment_ZeroDeposit_DoesNotContactProvider_Boundary()
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now); _context.Initial.Amount = 0;

        // Act
        var result = await _service.StartAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        result.CheckoutUrl.Should().BeNull();
        _gatewayMock.Verify(r => r.GetAsync(It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID06 PASSED: Payment_ZeroDeposit_DoesNotContactProvider (Boundary).");
    }

    // TCID07_Payment_ExactlyAtExpiry_ClosesInsteadOfCreatingLink_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID07_Payment_ExactlyAtExpiry_ClosesInsteadOfCreatingLink_Boundary()
    {
        // Arrange
        _context.Order.PaymentExpiresAt = _context.Clock.Now; _context.ProviderStatus = "CANCELLED";

        // Act
        var result = await _service.StartAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        result.Status.Should().Be("Expired");
        _gatewayMock.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(_context.Order, false, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID07 PASSED: Payment_ExactlyAtExpiry_ClosesInsteadOfCreatingLink (Boundary).");
    }

    // TCID08_Payment_ForeignOrder_ThrowsNotFound_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID08_Payment_ForeignOrder_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetOrderAsync(_context.Order.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = () => _service.StartAsync(userId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _gatewayMock.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID08 PASSED: Payment_ForeignOrder_ThrowsNotFound (Abnormal).");
    }
}
