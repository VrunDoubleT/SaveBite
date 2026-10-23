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
public class ViewOrderHistoryTests
{
    private readonly ITestOutputHelper _output;
    private readonly OrderReadTestContext _context;
    private readonly OrderService _service;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;

    public ViewOrderHistoryTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new OrderReadTestContext();
        _service = _context.Service;
        _orderRepoMock = _context.Repo;
    }

    // TCID01_ViewOrderHistory_ValidCustomer_ReturnsRecordedItemsAndCounts_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_ViewOrderHistory_ValidCustomer_ReturnsRecordedItemsAndCounts_Normal()
    {
        // Arrange
        var order = _context.Order;
        order.Items.First().FlashDealVariant.FlashDeal.Product.Name = "New catalog name";

        // Act
        var result = await _service.PageAsync(order.CustomerId, "All", null, 10);

        // Assert
        result.Page.Items.Should().ContainSingle();
        result.Page.Items[0].Items[0].ProductName.Should().Be("Purchased Product");
        result.Page.Items[0].TotalAmount.Should().Be(50000m);
        result.StatusCounts["Pending"].Should().Be(1);
        _orderRepoMock.Verify(r => r.GetOrderPageAsync(order.CustomerId, null, null, null, 10, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: ViewOrderHistory_ValidCustomer_ReturnsRecordedItemsAndCounts (Normal).");
    }

    // TCID02_ViewOrderHistory_StatusFilter_IsPassedToRepository_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID02_ViewOrderHistory_StatusFilter_IsPassedToRepository_Normal()
    {
        // Arrange
        var order = _context.Order;

        // Act
        await _service.PageAsync(order.CustomerId, "Pending", null, 10);

        // Assert
        _orderRepoMock.Verify(r => r.GetOrderPageAsync(order.CustomerId, OrderStatus.Pending, null, null, 10, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID02 PASSED: ViewOrderHistory_StatusFilter_IsPassedToRepository (Normal).");
    }

    // TCID03_ViewOrderHistory_PageSizeLimits_AreAccepted_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID03_ViewOrderHistory_PageSizeLimits_AreAccepted_Boundary(int size)
    {
        // Arrange
        var userId = _context.Order.CustomerId;

        // Act
        var result = await _service.PageAsync(userId, null, null, size);

        // Assert
        result.Page.PageSize.Should().Be(size);
        _output.WriteLine("TCID03 PASSED: ViewOrderHistory_PageSizeLimits_AreAccepted (Boundary).");
    }

    // TCID04_ViewOrderHistory_InvalidPageSize_ThrowsBadRequest_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID04_ViewOrderHistory_InvalidPageSize_ThrowsBadRequest_Boundary(int size)
    {
        // Arrange
        var userId = _context.Order.CustomerId;

        // Act
        Func<Task> act = () => _service.PageAsync(userId, "All", null, size);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("TCID04 PASSED: ViewOrderHistory_InvalidPageSize_ThrowsBadRequest (Boundary).");
    }

    // TCID05_ViewOrderHistory_InvalidStatusOrCursor_ThrowsBadRequest_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("pending", null)]
    [InlineData("999", null)]
    [InlineData("All", "%%%")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID05_ViewOrderHistory_InvalidStatusOrCursor_ThrowsBadRequest_Abnormal(string status, string? cursor)
    {
        // Arrange
        var userId = _context.Order.CustomerId;

        // Act
        Func<Task> act = () => _service.PageAsync(userId, status, cursor, 10);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _orderRepoMock.Verify(r => r.GetOrderPageAsync(It.IsAny<Guid>(), It.IsAny<OrderStatus?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID05 PASSED: ViewOrderHistory_InvalidStatusOrCursor_ThrowsBadRequest (Abnormal).");
    }

    // TCID06_ViewOrderHistory_ForeignCustomerCursor_RejectsReplay_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_ViewOrderHistory_ForeignCustomerCursor_RejectsReplay_Abnormal()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.GetOrderPageAsync(order.CustomerId, null, null, null, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Order>)new[] { order }, true, (IReadOnlyDictionary<string, int>)new Dictionary<string, int>()));
        var first = await _service.PageAsync(order.CustomerId, null, null, 1);

        // Act
        Func<Task> act = () => _service.PageAsync(Guid.NewGuid(), null, first.Page.NextCursor, 1);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("TCID06 PASSED: ViewOrderHistory_ForeignCustomerCursor_RejectsReplay (Abnormal).");
    }

    // TCID07_ViewOrderHistory_NextCursor_UsesRecordedTimeAndId_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID07_ViewOrderHistory_NextCursor_UsesRecordedTimeAndId_Normal()
    {
        // Arrange
        var order = _context.Order;
        _orderRepoMock.Setup(r => r.GetOrderPageAsync(order.CustomerId, null, null, null, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Order>)new[] { order }, true, (IReadOnlyDictionary<string, int>)new Dictionary<string, int>()));
        var first = await _service.PageAsync(order.CustomerId, null, null, 1);

        // Act
        await _service.PageAsync(order.CustomerId, null, first.Page.NextCursor, 1);

        // Assert
        _orderRepoMock.Verify(r => r.GetOrderPageAsync(order.CustomerId, null, order.CreatedAt, order.Id, 1, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID07 PASSED: ViewOrderHistory_NextCursor_UsesRecordedTimeAndId (Normal).");
    }

    // TCID08_ViewOrderHistory_EmptyHistory_ReturnsNoNextCursor_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID08_ViewOrderHistory_EmptyHistory_ReturnsNoNextCursor_Boundary()
    {
        // Arrange
        _orderRepoMock.Setup(r => r.GetOrderPageAsync(It.IsAny<Guid>(), It.IsAny<OrderStatus?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Order>(), false, (IReadOnlyDictionary<string, int>)new Dictionary<string, int>()));

        // Act
        var result = await _service.PageAsync(_context.Order.CustomerId, "All", null, 10);

        // Assert
        result.Page.Items.Should().BeEmpty(); result.Page.NextCursor.Should().BeNull();
        _output.WriteLine("TCID08 PASSED: ViewOrderHistory_EmptyHistory_ReturnsNoNextCursor (Boundary).");
    }
}
