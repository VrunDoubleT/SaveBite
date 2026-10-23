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
public class SearchOrderByIdTests
{
    private readonly ITestOutputHelper _output;
    private readonly ShopOrderTestContext _context;
    private readonly ShopOrderService _service;
    private readonly Mock<IShopOrderRepository> _shopRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<IShopOrderEventService> _realtimeMock;

    public SearchOrderByIdTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new ShopOrderTestContext();
        _service = _context.Service;
        _shopRepoMock = _context.Shops;
        _orderRepoMock = _context.Orders;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_SearchOrderById_OrderUuid_IsPassedExactlyWithinShop_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_SearchOrderById_OrderUuid_IsPassedExactlyWithinShop_Normal()
    {
        // Arrange
        var id = _context.Order.Id.ToString();

        // Act
        await _service.PageAsync(_context.ActorId, _context.Order.ShopId, null, id, 1, 10);

        // Assert
        _shopRepoMock.Verify(r => r.PageAsync(_context.Order.ShopId, null, id, 1, 10, It.IsAny<CancellationToken>(), null), Times.Once);
        _output.WriteLine("TCID01 PASSED: SearchOrderById_OrderUuid_IsPassedExactlyWithinShop (Normal).");
    }

    // TCID02_SearchOrderById_ShortPickupCode_IsPaddedAndScopedToToday_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID02_SearchOrderById_ShortPickupCode_IsPaddedAndScopedToToday_Normal()
    {
        // Arrange
        var expectedDate = DateOnly.FromDateTime(_context.Clock.Now.AddHours(7));

        // Act
        await _service.PageAsync(_context.ActorId, _context.Order.ShopId, "All", " 123 ", 1, 10);

        // Assert
        _shopRepoMock.Verify(r => r.PageAsync(_context.Order.ShopId, null, "000123", 1, 10, It.IsAny<CancellationToken>(), expectedDate), Times.Once);
        _output.WriteLine("TCID02 PASSED: SearchOrderById_ShortPickupCode_IsPaddedAndScopedToToday (Normal).");
    }

    // TCID03_SearchOrderById_ExplicitDate_PreservesHistoricalDate_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID03_SearchOrderById_ExplicitDate_PreservesHistoricalDate_Normal()
    {
        // Arrange
        var date = new DateOnly(2026, 10, 1);

        // Act
        await _service.PageAsync(_context.ActorId, _context.Order.ShopId, null, "000123", 1, 10, orderDate: date);

        // Assert
        _shopRepoMock.Verify(r => r.PageAsync(_context.Order.ShopId, null, "000123", 1, 10, It.IsAny<CancellationToken>(), date), Times.Once);
        _output.WriteLine("TCID03 PASSED: SearchOrderById_ExplicitDate_PreservesHistoricalDate (Normal).");
    }

    // TCID04_SearchOrderById_VietnamDayBoundary_UsesCorrectBusinessDate_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(16, 19)]
    [InlineData(17, 20)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID04_SearchOrderById_VietnamDayBoundary_UsesCorrectBusinessDate_Boundary(int hour, int day)
    {
        // Arrange
        _context.Clock.Now = new DateTime(2026, 10, 19, hour, 0, 0, DateTimeKind.Utc);
        var expectedDate = new DateOnly(2026, 10, day);

        // Act
        await _service.PageAsync(_context.ActorId, _context.Order.ShopId, null, "1", 1, 10);

        // Assert
        _shopRepoMock.Verify(r => r.PageAsync(_context.Order.ShopId, null, "000001", 1, 10, It.IsAny<CancellationToken>(), expectedDate), Times.Once);
        _output.WriteLine("TCID04 PASSED: SearchOrderById_VietnamDayBoundary_UsesCorrectBusinessDate (Boundary).");
    }

    // TCID05_SearchOrderById_EmptySearch_RemovesSearchFilter_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_SearchOrderById_EmptySearch_RemovesSearchFilter_Boundary()
    {
        // Arrange
        var shop = _context.Order.ShopId;

        // Act
        await _service.PageAsync(_context.ActorId, shop, "All", "   ", 1, 10);

        // Assert
        _shopRepoMock.Verify(r => r.PageAsync(shop, null, null, 1, 10, It.IsAny<CancellationToken>(), null), Times.Once);
        _output.WriteLine("TCID05 PASSED: SearchOrderById_EmptySearch_RemovesSearchFilter (Boundary).");
    }

    // TCID06_SearchOrderById_TooLongSearch_ThrowsBadRequest_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID06_SearchOrderById_TooLongSearch_ThrowsBadRequest_Boundary()
    {
        // Arrange
        var search = new string('a', 65);

        // Act
        Func<Task> act = () => _service.PageAsync(_context.ActorId, _context.Order.ShopId, null, search, 1, 10);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("TCID06 PASSED: SearchOrderById_TooLongSearch_ThrowsBadRequest (Boundary).");
    }

    // TCID07_SearchOrderById_OrderNotFound_ReturnsEmptyPage_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID07_SearchOrderById_OrderNotFound_ReturnsEmptyPage_Abnormal()
    {
        // Arrange
        _shopRepoMock.Setup(r => r.PageAsync(_context.Order.ShopId, null, "999999", 1, 10, It.IsAny<CancellationToken>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync((Array.Empty<Order>(), 0, (IReadOnlyDictionary<string, int>)new Dictionary<string, int>()));

        // Act
        var result = await _service.PageAsync(_context.ActorId, _context.Order.ShopId, null, "999999", 1, 10);

        // Assert
        result.Page.Items.Should().BeEmpty();
        _output.WriteLine("TCID07 PASSED: SearchOrderById_OrderNotFound_ReturnsEmptyPage (Abnormal).");
    }
}
