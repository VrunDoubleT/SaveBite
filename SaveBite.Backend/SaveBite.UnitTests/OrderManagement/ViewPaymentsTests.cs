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
public class ViewPaymentsTests
{
    private readonly ITestOutputHelper _output;
    private readonly PaymentHistoryTestContext _context;
    private readonly PaymentHistoryService _service;
    private readonly Mock<IPaymentHistoryRepository> _paymentRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;

    public ViewPaymentsTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new PaymentHistoryTestContext();
        _service = _context.Service;
        _paymentRepoMock = _context.Payments;
        _orderRepoMock = _context.Orders;
    }

    // TCID01_ViewPayments_ValidCustomer_ReturnsRecordedPaymentAndShop_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_ViewPayments_ValidCustomer_ReturnsRecordedPaymentAndShop_Normal()
    {
        // Arrange
        var order = _context.Order;

        // Act
        var result = await _service.PageAsync(order.CustomerId, 1, 10, "All");

        // Assert
        result.Items.Should().ContainSingle();
        result.Items[0].Amount.Should().Be(5000m);
        result.Items[0].CollectedAmount.Should().Be(5000m);
        result.Items[0].ShopName.Should().Be("Test Shop");
        _paymentRepoMock.Verify(r => r.RequireCustomerAsync(order.CustomerId, It.IsAny<CancellationToken>()), Times.Once);
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID01 PASSED: ViewPayments_ValidCustomer_ReturnsRecordedPaymentAndShop (Normal).");
    }

    // TCID02_ViewPayments_StatusFilter_IsPassedWithCustomerScope_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID02_ViewPayments_StatusFilter_IsPassedWithCustomerScope_Normal()
    {
        // Arrange
        var userId = _context.Order.CustomerId;

        // Act
        await _service.PageAsync(userId, 2, 10, "Succeeded");

        // Assert
        _paymentRepoMock.Verify(r => r.PageAsync(userId, PaymentStatus.Succeeded, 2, 10, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID02 PASSED: ViewPayments_StatusFilter_IsPassedWithCustomerScope (Normal).");
    }

    // TCID03_ViewPayments_InvalidPagination_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 31)]
    [InlineData(int.MaxValue, 30)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID03_ViewPayments_InvalidPagination_Rejects_Boundary(int page, int size)
    {
        // Arrange
        var userId = _context.Order.CustomerId;

        // Act
        Func<Task> act = () => _service.PageAsync(userId, page, size, "All");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _paymentRepoMock.Verify(r => r.PageAsync(It.IsAny<Guid>(), It.IsAny<PaymentStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID03 PASSED: ViewPayments_InvalidPagination_Rejects (Boundary).");
    }

    // TCID04_ViewPayments_MinimumAndMaximumPageSizes_AreAccepted_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID04_ViewPayments_MinimumAndMaximumPageSizes_AreAccepted_Boundary(int size)
    {
        // Arrange
        var userId = _context.Order.CustomerId;

        // Act
        var result = await _service.PageAsync(userId, 1, size, null);

        // Assert
        result.PageSize.Should().Be(size);
        _output.WriteLine("TCID04 PASSED: ViewPayments_MinimumAndMaximumPageSizes_AreAccepted (Boundary).");
    }

    // TCID05_ViewPayments_InactiveCustomer_ThrowsForbidden_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID05_ViewPayments_InactiveCustomer_ThrowsForbidden_Abnormal()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.RequireCustomerAsync(_context.Order.CustomerId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden());

        // Act
        Func<Task> act = () => _service.PageAsync(_context.Order.CustomerId, 1, 10, null);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _output.WriteLine("TCID05 PASSED: ViewPayments_InactiveCustomer_ThrowsForbidden (Abnormal).");
    }

    // TCID06_ViewPayments_InvalidStatus_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_ViewPayments_InvalidStatus_Rejects_Abnormal()
    {
        // Arrange
        var userId = _context.Order.CustomerId;

        // Act
        Func<Task> act = () => _service.PageAsync(userId, 1, 10, "succeeded");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("TCID06 PASSED: ViewPayments_InvalidStatus_Rejects (Abnormal).");
    }

    // TCID07_ViewPayments_EmptyHistory_ReturnsZeroItems_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID07_ViewPayments_EmptyHistory_ReturnsZeroItems_Boundary()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.PageAsync(It.IsAny<Guid>(), null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Payment>(), 0));

        // Act
        var result = await _service.PageAsync(_context.Order.CustomerId, 1, 10, "All");

        // Assert
        result.Items.Should().BeEmpty(); result.TotalItems.Should().Be(0);
        _output.WriteLine("TCID07 PASSED: ViewPayments_EmptyHistory_ReturnsZeroItems (Boundary).");
    }
}
