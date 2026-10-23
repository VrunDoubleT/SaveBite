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
public class ConfirmOrderTests
{
    private readonly ITestOutputHelper _output;
    private readonly ShopOrderTestContext _context;
    private readonly ShopOrderService _service;
    private readonly Mock<IShopOrderRepository> _shopRepoMock;
    private readonly Mock<ICheckoutRepository> _orderRepoMock;
    private readonly Mock<IShopOrderEventService> _realtimeMock;

    public ConfirmOrderTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new ShopOrderTestContext();
        _service = _context.Service;
        _shopRepoMock = _context.Shops;
        _orderRepoMock = _context.Orders;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_ConfirmOrder_PaidPending_RecordsActorAndSchedulesNoShow_Normal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_ConfirmOrder_PaidPending_RecordsActorAndSchedulesNoShow_Normal(bool staff)
    {
        // Arrange
        if (staff) _context.AsStaff();
        var order = _context.Order; order.ConfirmationExpirationJobId = "confirmation-job";

        // Act
        var result = await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "confirm");

        // Assert
        result.Order.Status.Should().Be("Confirmed");
        order.ConfirmedAt.Should().Be(_context.Clock.Now);
        result.History.Should().Contain(x => x.ToStatus == "Confirmed" && x.ActorId == _context.ActorId && x.ActorRole == (staff ? "Staff" : "StoreOwner"));
        _context.NoShow.Verify(r => r.Schedule(order.Id, order.HardDeadlineAt!.Value), Times.Once);
        _context.Confirmation.Verify(r => r.Delete("confirmation-job"), Times.Once);
        _shopRepoMock.Verify(r => r.AddActivity(It.Is<StaffActivityLog>(x => x.ActionBy == _context.ActorId && x.Action == OrderStatus.Confirmed)), Times.Once);
        _realtimeMock.Verify(r => r.NotifyAsync(order.ShopId, order.Id, RealtimeEvents.OrderReasons.Confirm, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: ConfirmOrder_PaidPending_RecordsActorAndSchedulesNoShow (Normal).");
    }

    // TCID02_ConfirmOrder_RepeatedConfirmation_DoesNotScheduleTwice_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID02_ConfirmOrder_RepeatedConfirmation_DoesNotScheduleTwice_Boundary()
    {
        // Arrange
        var order = _context.Order;
        await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "confirm");

        // Act
        await _service.ActionAsync(_context.ActorId, order.ShopId, order.Id, "confirm");

        // Assert
        _context.NoShow.Verify(r => r.Schedule(order.Id, It.IsAny<DateTime>()), Times.Once);
        order.StatusHistories.Count(x => x.ToStatus == OrderStatus.Confirmed).Should().Be(1);
        _output.WriteLine("TCID02 PASSED: ConfirmOrder_RepeatedConfirmation_DoesNotScheduleTwice (Boundary).");
    }

    // TCID03_ConfirmOrder_ExactlyFiveMinutesAfterPayment_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID03_ConfirmOrder_ExactlyFiveMinutesAfterPayment_Rejects_Boundary()
    {
        // Arrange
        _context.Initial.PaidAt = _context.Clock.Now.AddMinutes(-5);

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "confirm");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.Message.Should().Contain("five-minute");
        _context.NoShow.Verify(r => r.Schedule(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
        _output.WriteLine("TCID03 PASSED: ConfirmOrder_ExactlyFiveMinutesAfterPayment_Rejects (Boundary).");
    }

    // TCID04_ConfirmOrder_OneTickBeforeFiveMinutes_Succeeds_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID04_ConfirmOrder_OneTickBeforeFiveMinutes_Succeeds_Boundary()
    {
        // Arrange
        _context.Initial.PaidAt = _context.Clock.Now.AddMinutes(-5).AddTicks(1);

        // Act
        var result = await _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "confirm");

        // Assert
        result.Order.Status.Should().Be("Confirmed");
        _output.WriteLine("TCID04 PASSED: ConfirmOrder_OneTickBeforeFiveMinutes_Succeeds (Boundary).");
    }

    // TCID05_ConfirmOrder_UnverifiedDeposit_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID05_ConfirmOrder_UnverifiedDeposit_Rejects_Abnormal()
    {
        // Arrange
        _context.Initial.Status = PaymentStatus.Pending;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "confirm");

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID05 PASSED: ConfirmOrder_UnverifiedDeposit_Rejects (Abnormal).");
    }

    // TCID06_ConfirmOrder_TerminalOrLaterState_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.NoShow)]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_ConfirmOrder_TerminalOrLaterState_Rejects_Abnormal(OrderStatus state)
    {
        // Arrange
        _context.Order.Status = state;

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "confirm");

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _context.NoShow.Verify(r => r.Schedule(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
        _output.WriteLine("TCID06 PASSED: ConfirmOrder_TerminalOrLaterState_Rejects (Abnormal).");
    }

    // TCID07_ConfirmOrder_SchedulingFailure_DoesNotSaveOrNotify_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID07_ConfirmOrder_SchedulingFailure_DoesNotSaveOrNotify_Abnormal()
    {
        // Arrange
        _context.NoShow.Setup(r => r.Schedule(It.IsAny<Guid>(), It.IsAny<DateTime>())).Throws(new InvalidOperationException("Scheduler unavailable."));

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "confirm");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _realtimeMock.Verify(r => r.NotifyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID07 PASSED: ConfirmOrder_SchedulingFailure_DoesNotSaveOrNotify (Abnormal).");
    }

    // TCID08_ConfirmOrder_UnauthorizedOperator_RejectsBeforeMutation_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID08_ConfirmOrder_UnauthorizedOperator_RejectsBeforeMutation_Abnormal()
    {
        // Arrange

        _shopRepoMock.Setup(r => r.RequireAccessAsync(_context.ActorId, _context.Order.ShopId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden());

        // Act
        Func<Task> act = () => _service.ActionAsync(_context.ActorId, _context.Order.ShopId, _context.Order.Id, "confirm", "Closed");

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _orderRepoMock.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
        _realtimeMock.Verify(r => r.NotifyAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID08 PASSED: ConfirmOrder_UnauthorizedOperator_RejectsBeforeMutation (Abnormal).");
    }
}
