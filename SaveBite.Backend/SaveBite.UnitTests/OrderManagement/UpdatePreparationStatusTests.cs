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
public class UpdatePreparationStatusTests
{
    private readonly ITestOutputHelper _output;
    private readonly ShopOrderTestContext _context;
    private readonly ShopOrderService _service;
    private readonly Mock<IShopOrderRepository> _shopRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<IShopOrderEventService> _realtimeMock;

    public UpdatePreparationStatusTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new ShopOrderTestContext();
        _service = _context.Service;
        _shopRepoMock = _context.Shops;
        _orderRepoMock = _context.Orders;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_UpdatePreparationStatus_ForwardTransition_RecordsStaffAndNotifies_Normal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(OrderStatus.Confirmed, "Preparing", OrderStatus.Preparing)]
    [InlineData(OrderStatus.Preparing, "ReadyForPickup", OrderStatus.ReadyForPickup)]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_UpdatePreparationStatus_ForwardTransition_RecordsStaffAndNotifies_Normal(OrderStatus from, string action, OrderStatus expected)
    {
        // Arrange
        _context.AsStaff(); _context.Order.Status = from; var order = _context.Order;

        // Act
        var result = await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, action);

        // Assert
        result.Order.Status.Should().Be(expected.ToString());
        _shopRepoMock.Verify(r => r.AddActivity(It.Is<StaffActivityLog>(x => x.Action == expected && x.ActorRoleSnapshot == "Staff" && x.ActorNameSnapshot == "Test Staff")), Times.Once);
        _realtimeMock.Verify(r => r.NotifyAsync(order.ShopId, order.Id, action, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: UpdatePreparationStatus_ForwardTransition_RecordsStaffAndNotifies (Normal).");
    }

    // TCID02_UpdatePreparationStatus_ReadyForPickup_RecordsReadyTime_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID02_UpdatePreparationStatus_ReadyForPickup_RecordsReadyTime_Normal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.Preparing;

        // Act
        await _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "ReadyForPickup");

        // Assert
        _context.Order.ReadyAt.Should().Be(_context.Clock.Now);
        _output.WriteLine("TCID02 PASSED: UpdatePreparationStatus_ReadyForPickup_RecordsReadyTime (Normal).");
    }

    // TCID03_UpdatePreparationStatus_BackwardOrSkippedTransition_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(OrderStatus.Pending, "Preparing")]
    [InlineData(OrderStatus.Confirmed, "ReadyForPickup")]
    [InlineData(OrderStatus.ReadyForPickup, "Preparing")]
    [InlineData(OrderStatus.Completed, "Preparing")]
    [InlineData(OrderStatus.Cancelled, "ReadyForPickup")]
    [InlineData(OrderStatus.NoShow, "Preparing")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_UpdatePreparationStatus_BackwardOrSkippedTransition_Rejects_Abnormal(OrderStatus from, string action)
    {
        // Arrange
        _context.Order.Status = from;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, action);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID03 PASSED: UpdatePreparationStatus_BackwardOrSkippedTransition_Rejects (Abnormal).");
    }

    // TCID04_UpdatePreparationStatus_RepeatedReadyRequest_DoesNotDuplicateHistory_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID04_UpdatePreparationStatus_RepeatedReadyRequest_DoesNotDuplicateHistory_Boundary()
    {
        // Arrange
        _context.Order.Status = OrderStatus.Preparing;
        await _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "ReadyForPickup");

        // Act
        await _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "ReadyForPickup");

        // Assert
        _context.Order.StatusHistories.Count(x => x.ToStatus == OrderStatus.ReadyForPickup).Should().Be(1);
        _output.WriteLine("TCID04 PASSED: UpdatePreparationStatus_RepeatedReadyRequest_DoesNotDuplicateHistory (Boundary).");
    }

    // TCID05_UpdatePreparationStatus_AtDeadline_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_UpdatePreparationStatus_AtDeadline_Rejects_Boundary()
    {
        // Arrange
        _context.Order.Status = OrderStatus.Confirmed; _context.Order.HardDeadlineAt = _context.Clock.Now;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "Preparing");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _output.WriteLine("TCID05 PASSED: UpdatePreparationStatus_AtDeadline_Rejects (Boundary).");
    }

    // TCID06_UpdatePreparationStatus_PaymentReviewRequired_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_UpdatePreparationStatus_PaymentReviewRequired_Rejects_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.Confirmed; _context.Order.PaymentReviewRequired = true;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "Preparing");

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _shopRepoMock.Verify(r => r.AddActivity(It.IsAny<StaffActivityLog>()), Times.Never);
        _output.WriteLine("TCID06 PASSED: UpdatePreparationStatus_PaymentReviewRequired_Rejects (Abnormal).");
    }

    // TCID07_UpdatePreparationStatus_UnsupportedOperation_ThrowsBadRequest_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID07_UpdatePreparationStatus_UnsupportedOperation_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.Confirmed;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "Cooking");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("TCID07 PASSED: UpdatePreparationStatus_UnsupportedOperation_ThrowsBadRequest (Abnormal).");
    }

    // TCID08_UpdatePreparationStatus_UnauthorizedOperator_RejectsBeforeMutation_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID08_UpdatePreparationStatus_UnauthorizedOperator_RejectsBeforeMutation_Abnormal()
    {
        // Arrange
        _context.Order.Status = OrderStatus.Confirmed;
        _shopRepoMock.Setup(r => r.RequireAccessAsync(_context.ActorId, _context.Order.ShopId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden());

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "Preparing", "Closed");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _realtimeMock.Verify(r => r.NotifyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID08 PASSED: UpdatePreparationStatus_UnauthorizedOperator_RejectsBeforeMutation (Abnormal).");
    }
}
