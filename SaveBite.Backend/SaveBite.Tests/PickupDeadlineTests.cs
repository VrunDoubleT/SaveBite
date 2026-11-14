using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

public class PickupDeadlineTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.ReadyForPickup)]
    public async Task PaidRemainingCompletesAtDeadlineAndWarnsOwnerOnce(OrderStatus status)
    {
        var f = Fixture(status);
        await f.Service.MarkNoShowAsync(f.Order.Id);
        await f.Service.MarkNoShowAsync(f.Order.Id);
        Assert.Equal(OrderStatus.Completed, f.Order.Status);
        Assert.False(f.Order.NoShow);
        Assert.False(f.Order.IsLateArrival);
        Assert.Equal(Now, f.Order.CompletedAt);
        Assert.Equal(0, f.Order.RemainingAmount);
        Assert.Null(f.Order.ResaleDecisionDeadlineAt);
        Assert.Equal(1, f.Events.Completed);
        Assert.Equal(0, f.Events.NoShow);
        var notice = Assert.Single(f.Notifications.Notices);
        Assert.Contains(f.Order.Shop.OwnerUserId, notice.Target.UserIds!);
        Assert.Equal($"/store-owner/orders/{f.Order.Id}", notice.Command.ActionUrl);
        Assert.Contains("staff", notice.Command.Body);
        Assert.Equal($"order:{f.Order.Id}:auto-completed-remaining:owner", notice.Command.DeduplicationKey);
        var history = Assert.Single(f.Repository.History);
        Assert.Null(history.ChangedBy);
        Assert.Equal("System", history.ActorRoleSnapshot);
        Assert.Contains("Automatically completed", history.Note);
        Assert.Equal(RealtimeEvents.OrderReasons.Complete, Assert.Single(f.Realtime.Reasons));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FullUpfrontOrUnpaidRemainingStillBecomesNoShow(bool fullUpfront)
    {
        var f = Fixture();
        if (fullUpfront)
        {
            f.Order.PaymentMethod = PaymentMethod.OnlinePayment;
            f.Order.Payments = [new Payment { PaymentType = PaymentType.Full, Status = PaymentStatus.Succeeded, Amount = 10000 }];
        }
        else f.Order.Payments.Last().Status = PaymentStatus.Cancelled;
        await f.Service.MarkNoShowAsync(f.Order.Id);
        Assert.Equal(OrderStatus.NoShow, f.Order.Status);
        Assert.True(f.Order.NoShow);
        Assert.Equal(0, f.Events.Completed);
        Assert.Equal(1, f.Events.NoShow);
        Assert.Empty(f.Notifications.Notices);
    }

    [Fact]
    public async Task IncompleteRemainingDoesNotAutoComplete()
    {
        var f = Fixture();
        f.Order.Payments.Last().Amount = 7000;
        await f.Service.MarkNoShowAsync(f.Order.Id);
        Assert.Equal(OrderStatus.NoShow, f.Order.Status);
        Assert.Equal(1, f.Events.NoShow);
    }

    [Fact]
    public async Task MissedRemainingWebhookIsRecoveredBeforeDeadlineDecision()
    {
        var f = Fixture();
        var remaining = f.Order.Payments.Last();
        remaining.Status = PaymentStatus.Pending;
        remaining.GatewayCreationAttempted = true;
        f.Payments.OnReconcile = () => { remaining.Status = PaymentStatus.Succeeded; remaining.PaidAt = Now.AddMinutes(-5); };
        await f.Service.MarkNoShowAsync(f.Order.Id);
        Assert.Equal(1, f.Payments.Calls);
        Assert.Equal(OrderStatus.Completed, f.Order.Status);
        Assert.Equal(0, f.Events.NoShow);
    }

    [Fact]
    public async Task UnresolvedRemainingDefersNoShowAndDoesNotNotifyOwner()
    {
        var f = Fixture();
        f.Order.Payments.Last().Status = PaymentStatus.Pending;
        f.Order.Payments.Last().GatewayCreationAttempted = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.MarkNoShowAsync(f.Order.Id));
        Assert.Equal(OrderStatus.ReadyForPickup, f.Order.Status);
        Assert.Equal(0, f.Events.NoShow);
        Assert.Equal(0, f.Events.Completed);
        Assert.Empty(f.Notifications.Notices);
    }

    [Fact]
    public async Task PaymentReviewCannotPenalizeFullyPaidRemaining()
    {
        var f = Fixture();
        f.Order.PaymentReviewRequired = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.MarkNoShowAsync(f.Order.Id));
        Assert.Equal(OrderStatus.ReadyForPickup, f.Order.Status);
        Assert.Equal(0, f.Events.NoShow);
    }

    [Fact]
    public async Task DoesNotCompleteBeforeDeadlineOrOverwriteStaffCompletion()
    {
        var f = Fixture();
        f.Order.HardDeadlineAt = Now.AddMinutes(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.MarkNoShowAsync(f.Order.Id));
        f.Order.Status = OrderStatus.Completed;
        await f.Service.MarkNoShowAsync(f.Order.Id);
        Assert.Equal(0, f.Events.Completed);
        Assert.Empty(f.Notifications.Notices);
    }

    private static Setup Fixture(OrderStatus status = OrderStatus.ReadyForPickup)
    {
        var order = new Order { Id = Guid.NewGuid(), ShopId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), OrderCode = "000123",
            PaymentMethod = PaymentMethod.PayAtShop, Status = status, TotalAmount = 10000, RemainingAmount = 8000,
            HardDeadlineAt = Now.AddSeconds(-1), SoftDeadlineAt = Now.AddMinutes(-2),
            Shop = new Shop { OwnerUserId = Guid.NewGuid() },
            Payments = [new Payment { Id = Guid.NewGuid(), PaymentType = PaymentType.Deposit, Status = PaymentStatus.Succeeded, Amount = 2000 },
                new Payment { Id = Guid.NewGuid(), PaymentType = PaymentType.Remaining, Status = PaymentStatus.Succeeded, Amount = 8000, PaidAt = Now.AddMinutes(-5) }] };
        var repo = DispatchProxy.Create<ICheckoutRepository, Repository>();
        var repository = (Repository)(object)repo;
        repository.Order = order;
        var paymentService = DispatchProxy.Create<ICheckoutPaymentService, Payments>();
        var payments = (Payments)(object)paymentService;
        var notifications = new Notifications();
        var events = new Events();
        var realtime = new Realtime();
        var clock = new Clock();
        var resale = new OrderResaleService(null!, repo, null!, null!, clock, NullLogger<OrderResaleService>.Instance);
        var service = new ShopOrderService(null!, repo, paymentService, null!, null!, null!, clock,
            NullLogger<ShopOrderService>.Instance, null!, realtime, null!, events, null!, null!, resale, notifications);
        return new(service, order, repository, payments, notifications, events, realtime);
    }
    private sealed record Setup(ShopOrderService Service, Order Order, Repository Repository, Payments Payments, Notifications Notifications, Events Events, Realtime Realtime);
    public class Repository : DispatchProxy
    {
        public Order Order = null!;
        public List<OrderStatusHistory> History = [];
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "GetOrderAsync" => Task.FromResult<Order?>(Order),
            "WithOrderLockAsync" => ((Func<Task<Guid?>>)args![1]!)(),
            "AddHistory" => Add((OrderStatusHistory)args![0]!),
            "SaveAsync" => Task.CompletedTask,
            _ => throw new NotSupportedException(method.Name)
        };
        private object? Add(OrderStatusHistory history) { History.Add(history); return null; }
    }
    public class Payments : DispatchProxy
    {
        public int Calls;
        public Action? OnReconcile;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            Assert.Equal("ReconcilePickupDeadlineAsync", method!.Name);
            Calls++; OnReconcile?.Invoke(); return Task.FromResult(new CheckoutOrderResponse());
        }
    }
    private sealed class Events : IOrderEventPublisher
    {
        public int Completed, NoShow;
        public Task QueueCompletedAsync(Order order, CancellationToken ct = default) { Completed++; return Task.CompletedTask; }
        public Task QueueNoShowAsync(Order order, CancellationToken ct = default) { NoShow++; return Task.CompletedTask; }
    }
    private sealed class Notifications : INotificationQueue
    {
        public List<(NotificationTarget Target, CreateNotificationCommand Command)> Notices = [];
        public Task EnqueueAsync(NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default) { Notices.Add((target, command)); return Task.CompletedTask; }
    }
    private sealed class Realtime : IShopOrderEventService
    {
        public List<string> Reasons = [];
        public Task NotifyAsync(Guid shopId, Guid orderId, string reason, CancellationToken ct = default) { Reasons.Add(reason); return Task.CompletedTask; }
    }
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
}
