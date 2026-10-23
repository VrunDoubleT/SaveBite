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
public class ViewOrderDetailsTests
{
    private readonly ITestOutputHelper _output;
    private readonly PaymentTestContext _context;
    private readonly CheckoutPaymentService _service;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<ICheckoutPaymentGateway> _gatewayMock;

    public ViewOrderDetailsTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new PaymentTestContext();
        _service = _context.Service;
        _orderRepoMock = _context.Repo;
        _gatewayMock = _context.Gateway;
    }

    // TCID01_ViewOrderDetails_RecordedPurchase_ReturnsHistoricalPrices_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_ViewOrderDetails_RecordedPurchase_ReturnsHistoricalPrices_Normal()
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now, OrderStatus.Confirmed);
        var order = _context.Order; order.Items.First().FlashDealVariant.DealPrice = 99999;

        // Act
        var result = await _service.GetAsync(order.CustomerId, order.Id);

        // Assert
        result.OrderCode.Should().Be("000123");
        result.Items[0].UnitPrice.Should().Be(25000m);
        result.Items[0].Quantity.Should().Be(2);
        result.PaidAmount.Should().Be(5000m);
        result.OutstandingAmount.Should().Be(45000m);
        _orderRepoMock.Verify(r => r.GetOrderAsync(order.Id, order.CustomerId, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: ViewOrderDetails_RecordedPurchase_ReturnsHistoricalPrices (Normal).");
    }

    // TCID02_ViewOrderDetails_ForeignOrMissingOrder_ThrowsNotFound_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID02_ViewOrderDetails_ForeignOrMissingOrder_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var other = Guid.NewGuid(); var orderId = _context.Order.Id;
        _orderRepoMock.Setup(r => r.GetOrderAsync(orderId, other, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = () => _service.GetAsync(other, orderId);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _gatewayMock.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID02 PASSED: ViewOrderDetails_ForeignOrMissingOrder_ThrowsNotFound (Abnormal).");
    }

    // TCID03_ViewOrderDetails_InactiveCustomer_PropagatesForbidden_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_ViewOrderDetails_InactiveCustomer_PropagatesForbidden_Abnormal()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.GetOrderAsync(order.Id, order.CustomerId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden("Active Customer access is required."));

        // Act
        Func<Task> act = () => _service.GetAsync(order.CustomerId, order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _output.WriteLine("TCID03 PASSED: ViewOrderDetails_InactiveCustomer_PropagatesForbidden (Abnormal).");
    }

    // TCID04_ViewOrderDetails_PaidOrder_DoesNotSettleStockAgain_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID04_ViewOrderDetails_PaidOrder_DoesNotSettleStockAgain_Normal()
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now);

        // Act
        await _service.GetAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        _orderRepoMock.Verify(r => r.CompleteInventoryAsync(It.IsAny<Order>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _gatewayMock.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID04 PASSED: ViewOrderDetails_PaidOrder_DoesNotSettleStockAgain (Normal).");
    }

    // TCID05_ViewOrderDetails_AtPickupDeadline_CancellationUnavailable_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_ViewOrderDetails_AtPickupDeadline_CancellationUnavailable_Boundary()
    {
        // Arrange
        _context.Order = UnitTestData.Order(_context.Clock.Now); _context.Order.HardDeadlineAt = _context.Clock.Now;

        // Act
        var result = await _service.GetAsync(_context.Order.CustomerId, _context.Order.Id);

        // Assert
        result.CanCancel.Should().BeFalse();
        result.CancellationUnavailableReason.Should().Contain("deadline");
        _output.WriteLine("TCID05 PASSED: ViewOrderDetails_AtPickupDeadline_CancellationUnavailable (Boundary).");
    }
}
