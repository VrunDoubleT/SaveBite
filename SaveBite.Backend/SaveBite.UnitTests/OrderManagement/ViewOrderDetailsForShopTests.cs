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
public class ViewOrderDetailsForShopTests
{
    private readonly ITestOutputHelper _output;
    private readonly ShopOrderTestContext _context;
    private readonly ShopOrderService _service;
    private readonly Mock<IShopOrderRepository> _shopRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<IShopOrderEventService> _realtimeMock;

    public ViewOrderDetailsForShopTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new ShopOrderTestContext();
        _service = _context.Service;
        _shopRepoMock = _context.Shops;
        _orderRepoMock = _context.Orders;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_ViewOrderDetailsForShop_ActiveOwnerOrStaff_SeesRecordedData_Normal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_ViewOrderDetailsForShop_ActiveOwnerOrStaff_SeesRecordedData_Normal(bool staff)
    {
        // Arrange
        if (staff) _context.AsStaff();
        var order = _context.Order; order.Items.First().FlashDealVariant.DealPrice = 99999;

        // Act
        var result = await _service.GetAsync(_context.ActorId, order.ShopId, order.Id);

        // Assert
        result.IsOwner.Should().Be(!staff);
        result.CustomerName.Should().Be("Test Customer");
        result.Order.Items[0].UnitPrice.Should().Be(25000m);
        result.Payments.Should().ContainSingle();
        _context.Payments.Verify(r => r.ReconcileOrderAsync(order.Id, false, It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID01 PASSED: ViewOrderDetailsForShop_ActiveOwnerOrStaff_SeesRecordedData (Normal).");
    }

    // TCID02_ViewOrderDetailsForShop_ForeignShop_RejectsBeforeRead_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID02_ViewOrderDetailsForShop_ForeignShop_RejectsBeforeRead_Abnormal()
    {
        // Arrange
        var otherShop = Guid.NewGuid();
        _shopRepoMock.Setup(r => r.RequireAccessAsync(_context.ActorId, otherShop, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden());

        // Act
        Func<Task> act = () => _service.GetAsync(_context.ActorId, otherShop, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _shopRepoMock.Verify(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID02 PASSED: ViewOrderDetailsForShop_ForeignShop_RejectsBeforeRead (Abnormal).");
    }

    // TCID03_ViewOrderDetailsForShop_MissingOrder_ThrowsNotFound_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_ViewOrderDetailsForShop_MissingOrder_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _shopRepoMock.Setup(r => r.GetAsync(_context.Order.ShopId, _context.Order.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        Func<Task> act = () => _service.GetAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _output.WriteLine("TCID03 PASSED: ViewOrderDetailsForShop_MissingOrder_ThrowsNotFound (Abnormal).");
    }

    // TCID04_ViewOrderDetailsForShop_AwaitingPayment_IsNotVisible_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID04_ViewOrderDetailsForShop_AwaitingPayment_IsNotVisible_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.AwaitingPayment;

        // Act
        Func<Task> act = () => _service.GetAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _context.Payments.Verify(r => r.ReconcileOrderAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID04 PASSED: ViewOrderDetailsForShop_AwaitingPayment_IsNotVisible (Abnormal).");
    }

    // TCID05_ViewOrderDetailsForShop_ExpiredBeforeSubmission_IsNotVisible_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_ViewOrderDetailsForShop_ExpiredBeforeSubmission_IsNotVisible_Boundary()
    {
        // Arrange
        _context.Order.Status = OrderStatus.Expired; _context.Order.StatusHistories.Clear();

        // Act
        Func<Task> act = () => _service.GetAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _output.WriteLine("TCID05 PASSED: ViewOrderDetailsForShop_ExpiredBeforeSubmission_IsNotVisible (Boundary).");
    }

    // TCID06_ViewOrderDetailsForShop_PaymentHistory_ShowsClosedAndActiveQrAttempts_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID06_ViewOrderDetailsForShop_PaymentHistory_ShowsClosedAndActiveQrAttempts_Normal()
    {
        // Arrange
        var old = UnitTestData.Remaining(_context.Order, _context.Clock.Now.AddMinutes(-1), PaymentStatus.Expired);
        var current = UnitTestData.Remaining(_context.Order, _context.Clock.Now);

        // Act
        var result = await _service.GetAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id);

        // Assert
        result.Payments.Should().HaveCount(3);
        result.Payments.Should().Contain(x => x.Id == old.Id && x.CheckoutUrl == null);
        result.Payments.Should().Contain(x => x.Id == current.Id && x.CheckoutUrl != null);
        _output.WriteLine("TCID06 PASSED: ViewOrderDetailsForShop_PaymentHistory_ShowsClosedAndActiveQrAttempts (Normal).");
    }
}
