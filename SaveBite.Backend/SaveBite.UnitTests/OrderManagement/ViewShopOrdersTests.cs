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
public class ViewShopOrdersTests
{
    private readonly ITestOutputHelper _output;
    private readonly ShopOrderTestContext _context;
    private readonly ShopOrderService _service;
    private readonly Mock<IShopOrderRepository> _shopRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<IShopOrderEventService> _realtimeMock;

    public ViewShopOrdersTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new ShopOrderTestContext();
        _service = _context.Service;
        _shopRepoMock = _context.Shops;
        _orderRepoMock = _context.Orders;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_ViewShopOrders_ActiveOwnerOrStaff_ReturnsScopedPage_Normal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_ViewShopOrders_ActiveOwnerOrStaff_ReturnsScopedPage_Normal(bool staff)
    {
        // Arrange
        if (staff) _context.AsStaff();
        var order = _context.Order;

        // Act
        var result = await _service.PageAsync(_context.ActorId, order.ShopId, "All", null, 1, 10);

        // Assert
        result.Page.Items.Should().ContainSingle();
        result.Page.Items[0].IsOwner.Should().Be(!staff);
        result.Page.TotalItems.Should().Be(1);
        _shopRepoMock.Verify(r => r.RequireAccessAsync(_context.ActorId, order.ShopId, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.PageAsync(order.ShopId, null, null, 1, 10, It.IsAny<CancellationToken>(), null), Times.Once);
        _output.WriteLine("TCID01 PASSED: ViewShopOrders_ActiveOwnerOrStaff_ReturnsScopedPage (Normal).");
    }

    // TCID02_ViewShopOrders_StatusFilter_PreservesRequestedPage_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID02_ViewShopOrders_StatusFilter_PreservesRequestedPage_Normal()
    {
        // Arrange
        var order = _context.Order;

        // Act
        await _service.PageAsync(_context.ActorId, order.ShopId, "Pending", null, 2, 10);

        // Assert
        _shopRepoMock.Verify(r => r.PageAsync(order.ShopId, "Pending", null, 2, 10, It.IsAny<CancellationToken>(), null), Times.Once);
        _output.WriteLine("TCID02 PASSED: ViewShopOrders_StatusFilter_PreservesRequestedPage (Normal).");
    }

    // TCID03_ViewShopOrders_UnauthorizedOperator_IsRejectedBeforeQuery_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_ViewShopOrders_UnauthorizedOperator_IsRejectedBeforeQuery_Abnormal()
    {
        // Arrange
        _shopRepoMock.Setup(r => r.RequireAccessAsync(_context.ActorId, _context.Order.ShopId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden());

        // Act
        Func<Task> act = () => _service.PageAsync(_context.ActorId, _context.Order.ShopId, null, null, 1, 10);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _shopRepoMock.Verify(r => r.PageAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<DateOnly?>()), Times.Never);
        _output.WriteLine("TCID03 PASSED: ViewShopOrders_UnauthorizedOperator_IsRejectedBeforeQuery (Abnormal).");
    }

    // TCID04_ViewShopOrders_InvalidPagination_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 31)]
    [InlineData(int.MaxValue, 30)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID04_ViewShopOrders_InvalidPagination_Rejects_Boundary(int page, int size)
    {
        // Arrange
        var shop = _context.Order.ShopId;

        // Act
        Func<Task> act = () => _service.PageAsync(_context.ActorId, shop, null, null, page, size);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("TCID04 PASSED: ViewShopOrders_InvalidPagination_Rejects (Boundary).");
    }

    // TCID05_ViewShopOrders_PageSizeBoundaries_Accepted_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_ViewShopOrders_PageSizeBoundaries_Accepted_Boundary(int size)
    {
        // Arrange
        var shop = _context.Order.ShopId;

        // Act
        var result = await _service.PageAsync(_context.ActorId, shop, null, null, 1, size);

        // Assert
        result.Page.PageSize.Should().Be(size);
        _output.WriteLine("TCID05 PASSED: ViewShopOrders_PageSizeBoundaries_Accepted (Boundary).");
    }

    // TCID06_ViewShopOrders_InvalidStatus_ThrowsBadRequest_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_ViewShopOrders_InvalidStatus_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var shop = _context.Order.ShopId;

        // Act
        Func<Task> act = () => _service.PageAsync(_context.ActorId, shop, "pending", null, 1, 10);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("TCID06 PASSED: ViewShopOrders_InvalidStatus_ThrowsBadRequest (Abnormal).");
    }

    // TCID07_ViewShopOrders_EmptyShop_ReturnsEmptyPage_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID07_ViewShopOrders_EmptyShop_ReturnsEmptyPage_Boundary()
    {
        // Arrange
        _shopRepoMock.Setup(r => r.PageAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync((Array.Empty<Order>(), 0, (IReadOnlyDictionary<string, int>)new Dictionary<string, int>()));

        // Act
        var result = await _service.PageAsync(_context.ActorId, _context.Order.ShopId, "All", null, 1, 10);

        // Assert
        result.Page.Items.Should().BeEmpty(); result.Page.TotalItems.Should().Be(0);
        _output.WriteLine("TCID07 PASSED: ViewShopOrders_EmptyShop_ReturnsEmptyPage (Boundary).");
    }
}
