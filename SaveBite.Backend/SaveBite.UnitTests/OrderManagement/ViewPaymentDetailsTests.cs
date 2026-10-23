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
public class ViewPaymentDetailsTests
{
    private readonly ITestOutputHelper _output;
    private readonly PaymentHistoryTestContext _context;
    private readonly PaymentHistoryService _service;
    private readonly Mock<IPaymentHistoryRepository> _paymentRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;

    public ViewPaymentDetailsTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new PaymentHistoryTestContext();
        _service = _context.Service;
        _paymentRepoMock = _context.Payments;
        _orderRepoMock = _context.Orders;
    }

    // TCID01_ViewPaymentDetails_OwnPayment_IncludesHistoricalOrder_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_ViewPaymentDetails_OwnPayment_IncludesHistoricalOrder_Normal()
    {
        // Arrange
        var order = _context.Order; var payment = order.Payments.First();

        // Act
        var result = await _service.GetAsync(order.CustomerId, payment.Id);

        // Assert
        result.Payment.Id.Should().Be(payment.Id);
        result.Order.OrderCode.Should().Be("000123");
        result.Order.Items[0].ProductName.Should().Be("Purchased Product");
        result.Order.PaidAmount.Should().Be(5000m);
        _orderRepoMock.Verify(r => r.GetOrderAsync(order.Id, order.CustomerId, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: ViewPaymentDetails_OwnPayment_IncludesHistoricalOrder (Normal).");
    }

    // TCID02_ViewPaymentDetails_ForeignOrMissingPayment_ThrowsNotFound_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID02_ViewPaymentDetails_ForeignOrMissingPayment_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        _paymentRepoMock.Setup(r => r.GetAsync(_context.Order.CustomerId, paymentId, It.IsAny<CancellationToken>())).ReturnsAsync((Payment?)null);

        // Act
        Func<Task> act = () => _service.GetAsync(_context.Order.CustomerId, paymentId);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _orderRepoMock.Verify(r => r.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID02 PASSED: ViewPaymentDetails_ForeignOrMissingPayment_ThrowsNotFound (Abnormal).");
    }

    // TCID03_ViewPaymentDetails_OrderOwnershipRecheckFails_ThrowsNotFound_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_ViewPaymentDetails_OrderOwnershipRecheckFails_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _orderRepoMock.Setup(r => r.GetOrderAsync(_context.Order.Id, _context.Order.CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = () => _service.GetAsync(_context.Order.CustomerId, _context.Order.Payments.First().Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.Message.Should().Be("Order not found.");
        _output.WriteLine("TCID03 PASSED: ViewPaymentDetails_OrderOwnershipRecheckFails_ThrowsNotFound (Abnormal).");
    }

    // TCID04_ViewPaymentDetails_CashReceipt_DisplaysCollectedCash_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID04_ViewPaymentDetails_CashReceipt_DisplaysCollectedCash_Normal()
    {
        // Arrange
        var payment = _context.Order.Payments.First(); payment.Method = PaymentMethod.Cash;

        // Act
        var result = await _service.GetAsync(_context.Order.CustomerId, payment.Id);

        // Assert
        result.Payment.Method.Should().Be("Cash");
        result.Payment.CollectedAmount.Should().Be(5000m);
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID04 PASSED: ViewPaymentDetails_CashReceipt_DisplaysCollectedCash (Normal).");
    }

    // TCID05_ViewPaymentDetails_PartialCollection_PreservesVerifiedAmount_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_ViewPaymentDetails_PartialCollection_PreservesVerifiedAmount_Boundary()
    {
        // Arrange
        var payment = _context.Order.Payments.First(); payment.Status = PaymentStatus.Pending; payment.CollectedAmount = 1000;

        // Act
        var result = await _service.GetAsync(_context.Order.CustomerId, payment.Id);

        // Assert
        result.Payment.Amount.Should().Be(5000m);
        result.Payment.CollectedAmount.Should().Be(1000m);
        _output.WriteLine("TCID05 PASSED: ViewPaymentDetails_PartialCollection_PreservesVerifiedAmount (Boundary).");
    }

    // TCID06_ViewPaymentDetails_InactiveCustomer_DoesNotReadPayment_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_ViewPaymentDetails_InactiveCustomer_DoesNotReadPayment_Abnormal()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.RequireCustomerAsync(_context.Order.CustomerId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden());

        // Act
        Func<Task> act = () => _service.GetAsync(_context.Order.CustomerId, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _paymentRepoMock.Verify(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID06 PASSED: ViewPaymentDetails_InactiveCustomer_DoesNotReadPayment (Abnormal).");
    }
}
