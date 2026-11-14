using SaveBite.Backend.Audit;
using System.Reflection;
using System.Text.Json;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Implementations;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

public class CheckoutSessionTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);

    private static Order Attempt() => new()
    {
        Id = Guid.NewGuid(), CustomerId = Guid.NewGuid(), ShopId = Guid.NewGuid(),
        ShopNameSnapshot = "Test shop", SnapshotCapturedAt = Now, OrderCode = "000001",
        CheckoutQuoteId = Guid.NewGuid(), CheckoutRequestId = Guid.NewGuid(),
        Status = OrderStatus.AwaitingPayment, TotalAmount = 20000, RequiredOnlineAmount = 20000,
        PaymentExpiresAt = Now.AddMinutes(2), HardDeadlineAt = Now.AddMinutes(30),
        SoftDeadlineAt = Now.AddMinutes(20), InventoryReservationActive = true, CreatedAt = Now,
        Shop = new Shop { Name = "Live shop name", PaymentConfig = new ShopPaymentConfig { PayosApiKey = "secret-key" } },
        Items = [new OrderItem { Id = Guid.NewGuid(), FlashDealVariantId = Guid.NewGuid(), ProductNameSnapshot = "Rice", Quantity = 2, UnitPrice = 10000 }],
        Payments = [new Payment { Id = Guid.NewGuid(), Status = PaymentStatus.Pending, Amount = 20000,
            PaymentType = PaymentType.Full, Method = PaymentMethod.OnlinePayment, CreatedAt = Now }]
    };

    [Fact]
    public void UnpaidCheckoutTracksSessionAndPaymentWithoutAnyOrderOrOrderItem()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options);
        var repository = new CheckoutRepository(db, null!);
        var attempt = Attempt();
        repository.AddOrder(attempt);
        Assert.Empty(db.ChangeTracker.Entries<Order>());
        Assert.Empty(db.ChangeTracker.Entries<OrderItem>());
        Assert.Single(db.ChangeTracker.Entries<CheckoutSession>());
        var payment = Assert.Single(db.ChangeTracker.Entries<Payment>()).Entity;
        Assert.Null(payment.OrderId);
        Assert.Equal(attempt.Id, payment.CheckoutSessionId);

        // Only verification promotes the persisted purchase snapshot into a real order graph.
        attempt.Status = OrderStatus.Pending;
        payment.Status = PaymentStatus.Succeeded;
        typeof(CheckoutRepository).GetMethod("PersistSessions", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(repository, null);
        Assert.Single(db.ChangeTracker.Entries<Order>());
        Assert.Single(db.ChangeTracker.Entries<OrderItem>());
        Assert.Equal(attempt.Id, payment.OrderId);
    }

    [Fact]
    public void SessionSnapshotRetainsPurchaseDataWithoutLiveNavigationGraphsOrCredentials()
    {
        var source = Attempt();
        var json = CheckoutSessionSnapshot.Serialize(source);
        Assert.DoesNotContain("secret-key", json);
        Assert.DoesNotContain("Live shop name", json);
        var restored = CheckoutSessionSnapshot.Read(json);
        Assert.Equal(source.CheckoutRequestId, restored.CheckoutRequestId);
        Assert.Equal(source.PaymentExpiresAt, restored.PaymentExpiresAt);
        Assert.Equal(2, Assert.Single(restored.Items).Quantity);
        Assert.Empty(restored.Payments);
        Assert.Equal(source.ShopNameSnapshot, restored.ShopNameSnapshot);
    }

    [Theory]
    [InlineData(true, PaymentStatus.Cancelled, NotificationType.PaymentCancelled)]
    [InlineData(false, PaymentStatus.Expired, NotificationType.PaymentExpired)]
    public async Task ClosedUnpaidCheckoutRestoresCartAndNotifiesOnlyOnce(bool cancel, PaymentStatus expected, NotificationType notification)
    {
        var attempt = Attempt();
        attempt.PaymentExpiresAt = Now.AddSeconds(-1);
        var repository = DispatchProxy.Create<ICheckoutRepository, RepositoryProxy>();
        var proxy = (RepositoryProxy)(object)repository;
        proxy.Order = attempt;
        var notifications = new Notifications();
        var service = new CheckoutPaymentService(repository, new DevelopmentCheckoutGateway(), new Inventory(), new Clock(),
            null!, NullLogger<CheckoutPaymentService>.Instance, null!, null!, notifications, null!, null!, null!);
        if (cancel)
        {
            await service.CancelAsync(attempt.CustomerId, attempt.Id);
            await service.CancelAsync(attempt.CustomerId, attempt.Id);
        }
        else
        {
            await service.ExpireAsync(attempt.Id);
            await service.ExpireAsync(attempt.Id);
        }
        Assert.Equal(expected, attempt.Payments.Single().Status);
        Assert.Null(attempt.Payments.Single().OrderId);
        Assert.False(attempt.InventoryReservationActive);
        Assert.Equal(1, proxy.Restores);
        Assert.Equal(notification, Assert.Single(notifications.Commands).Type);
        Assert.Equal("/cart", notifications.Commands[0].ActionUrl);
    }

    [Theory]
    [InlineData("CANCELLED", false, PaymentStatus.Cancelled)]
    [InlineData("EXPIRED", false, PaymentStatus.Expired)]
    [InlineData("PENDING", true, PaymentStatus.Expired)]
    public async Task ProviderClosureAndDeadlineRestoreCartWithoutSubmittingOrder(string providerStatus, bool expired, PaymentStatus expected)
    {
        var attempt = Attempt();
        if (expired) attempt.PaymentExpiresAt = Now.AddSeconds(-1);
        var payment = attempt.Payments.Single();
        payment.GatewayCreationAttempted = true;
        payment.GatewayTransactionId = DevelopmentCheckoutGateway.Prefix + providerStatus;
        var repository = DispatchProxy.Create<ICheckoutRepository, RepositoryProxy>();
        var proxy = (RepositoryProxy)(object)repository;
        proxy.Order = attempt;
        var notifications = new Notifications();
        var service = new CheckoutPaymentService(repository, new DevelopmentCheckoutGateway(), new Inventory(), new Clock(),
            null!, NullLogger<CheckoutPaymentService>.Instance, null!, null!, notifications, null!, null!, null!);
        await service.ReconcileAsync(attempt.CustomerId, attempt.Id);
        await service.ReconcileAsync(attempt.CustomerId, attempt.Id);
        Assert.Equal(expected, payment.Status);
        Assert.Null(payment.OrderId);
        Assert.Equal(1, proxy.Restores);
        Assert.Single(notifications.Commands);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingProviderPaymentReleasesReservationAndRestoresCartExactlyOnce(bool cancel)
    {
        var attempt = Attempt();
        attempt.PaymentExpiresAt = Now.AddSeconds(-1);
        var payment = attempt.Payments.Single();
        payment.GatewayCreationAttempted = true;
        payment.GatewayOrderCode = 123456;
        var repository = DispatchProxy.Create<ICheckoutRepository, RepositoryProxy>();
        var proxy = (RepositoryProxy)(object)repository;
        proxy.Order = attempt;
        var notifications = new Notifications();
        var service = new CheckoutPaymentService(repository, new MissingGateway(), new Inventory(), new Clock(),
            null!, NullLogger<CheckoutPaymentService>.Instance, null!, null!, notifications, null!, null!, null!);
        if (cancel) await service.CancelAsync(attempt.CustomerId, attempt.Id);
        else await service.ExpireAsync(attempt.Id);
        await service.ReconcileOrderAsync(attempt.Id);
        Assert.Equal(cancel ? PaymentStatus.Cancelled : PaymentStatus.Expired, payment.Status);
        Assert.False(attempt.InventoryReservationActive);
        Assert.Equal(1, proxy.Releases);
        Assert.Equal(1, proxy.Restores);
        Assert.Single(notifications.Commands);
    }

    [Fact]
    public async Task UncertainProviderFailureDoesNotReleasePotentiallyPayableStock()
    {
        var attempt = Attempt();
        attempt.PaymentExpiresAt = Now.AddSeconds(-1);
        attempt.Payments.Single().GatewayCreationAttempted = true;
        var repository = DispatchProxy.Create<ICheckoutRepository, RepositoryProxy>();
        var proxy = (RepositoryProxy)(object)repository;
        proxy.Order = attempt;
        var service = new CheckoutPaymentService(repository, new MissingGateway { Fail = true }, new Inventory(), new Clock(),
            null!, NullLogger<CheckoutPaymentService>.Instance, null!, null!, new Notifications(), null!, null!, null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExpireAsync(attempt.Id));
        Assert.Equal(PaymentStatus.Pending, attempt.Payments.Single().Status);
        Assert.True(attempt.InventoryReservationActive);
        Assert.Equal(0, proxy.Releases);
        Assert.Equal(0, proxy.Restores);
    }

    [Theory]
    [InlineData("PAID", "PAID", PaymentStatus.Succeeded, 0)]
    [InlineData("PENDING", "CANCELLED", PaymentStatus.Cancelled, 1)]
    [InlineData("PENDING", "PAID", PaymentStatus.Succeeded, 1)]
    [InlineData("PENDING", "PENDING", PaymentStatus.Pending, 1)]
    [InlineData("NOT_FOUND", "NOT_FOUND", PaymentStatus.Cancelled, 0)]
    public async Task PickupDeadlineVerifiesRemainingBeforeClosureWithoutFalseLateReview(
        string state, string closedState, PaymentStatus expected, int cancelCalls)
    {
        var order = Attempt();
        order.Status = OrderStatus.ReadyForPickup;
        order.PaymentMethod = PaymentMethod.PayAtShop;
        order.HardDeadlineAt = Now.AddSeconds(-1);
        order.Payments.Single().Status = PaymentStatus.Succeeded;
        order.Payments.Single().PaymentType = PaymentType.Deposit;
        order.Payments.Single().Amount = 2000;
        var remaining = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 18000,
            PaymentType = PaymentType.Remaining, Status = PaymentStatus.Pending, GatewayCreationAttempted = true };
        order.Payments.Add(remaining);
        var repo = DispatchProxy.Create<ICheckoutRepository, RepositoryProxy>();
        ((RepositoryProxy)(object)repo).Order = order;
        var gateway = new CutoffGateway(state, closedState);
        var service = new CheckoutPaymentService(repo, gateway, new Inventory(), new Clock(),
            null!, NullLogger<CheckoutPaymentService>.Instance, null!, null!, new Notifications(), null!, null!, null!);
        await service.ReconcilePickupDeadlineAsync(order.Id);
        Assert.Equal(expected, remaining.Status);
        Assert.Equal(cancelCalls, gateway.Cancels);
        Assert.False(order.PaymentReviewRequired);
        Assert.Equal(OrderStatus.ReadyForPickup, order.Status);
    }

    private sealed class CutoffGateway(string state, string closedState) : ICheckoutPaymentGateway
    {
        public int Cancels;
        public bool IsConfigured(ShopPaymentConfig? config) => true;
        public bool VerifyWebhook(JsonElement body, ShopPaymentConfig config) => false;
        private static Task<GatewayPayment> Result(Payment payment, string status) => Task.FromResult(new GatewayPayment(
            status, payment.GatewayOrderCode ?? 0, payment.Amount, status == "PAID" ? payment.Amount : 0, null, null));
        public Task<GatewayPayment> GetAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct) => Result(payment, state);
        public Task<GatewayPayment> CancelAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct) { Cancels++; return Result(payment, closedState); }
        public Task<GatewayPayment> CreateAsync(Order order, Payment payment, ShopPaymentConfig config, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class MissingGateway : ICheckoutPaymentGateway
    {
        public bool Fail;
        public bool IsConfigured(ShopPaymentConfig? config) => true;
        public bool VerifyWebhook(JsonElement body, ShopPaymentConfig config) => false;
        public Task<GatewayPayment> GetAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct) => Fail
            ? throw AppException.ServiceUnavailable("PayOS temporarily unavailable.")
            : Task.FromResult(new GatewayPayment("NOT_FOUND", payment.GatewayOrderCode ?? 0, payment.Amount, 0, null, null));
        public Task<GatewayPayment> CreateAsync(Order order, Payment payment, ShopPaymentConfig config, CancellationToken ct) => throw new NotSupportedException();
        public Task<GatewayPayment> CancelAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct) => throw new NotSupportedException();
    }

    [Fact]
    public async Task OrderDetailApiDoesNotExposeAnUnpaidCheckoutSession()
    {
        var repository = DispatchProxy.Create<ICheckoutRepository, RepositoryProxy>();
        var proxy = (RepositoryProxy)(object)repository;
        proxy.Order = Attempt();
        var service = new OrderService(repository, null!, new Clock());
        await Assert.ThrowsAsync<SaveBite.Backend.Exceptions.AppException>(() =>
            service.GetAsync(proxy.Order.CustomerId, proxy.Order.Id));
        proxy.Order.Payments.Single().OrderId = proxy.Order.Id;
        proxy.Order.Payments.Single().Status = PaymentStatus.Succeeded;
        proxy.Order.Status = OrderStatus.Pending;
        var result = await service.GetAsync(proxy.Order.CustomerId, proxy.Order.Id);
        Assert.Equal(proxy.Order.Id, result.Id);
        Assert.False(result.IsCheckoutSession);
    }

    [Fact]
    public void VerifiedRemainingPaymentStagesNoticesForCustomerOwnerAndStaff()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options);
        var order = Attempt();
        OrderNotificationOutbox.StageRemainingPayment(db, order);
        var notices = db.ChangeTracker.Entries<OrderEventOutbox>().Select(e => e.Entity).ToList();
        Assert.Equal(3, notices.Count);
        Assert.Contains(notices, n => n.EventKey.EndsWith(":owner"));
        Assert.Contains(notices, n => n.EventKey.EndsWith(":staff"));
        Assert.Contains(notices, n => n.EventKey.EndsWith(":customer"));
        Assert.All(notices, n => Assert.Equal(order.Id, n.OrderId));
    }

    [Fact]
    public async Task SignedRemainingWebhookSettlesWithoutPollingAndDuplicateDoesNotRepublish()
    {
        var (service, order, gateway, realtime) = WebhookFixture();
        var body = SignedWebhook(order.Payments.Last().GatewayOrderCode!.Value, 10000);
        await service.WebhookAsync(body);
        await service.WebhookAsync(body);
        var payment = order.Payments.Last();
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(10000, payment.CollectedAmount);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(0, gateway.Reads);
        var evt = Assert.Single(realtime.Events);
        Assert.Equal(RealtimeEvents.PaymentChanged, evt.Type);
        Assert.Equal(payment.Id, evt.Data.GetProperty("PaymentId").GetGuid());
    }

    [Fact]
    public async Task SignedInitialWebhookSubmitsOrderWithoutPolling()
    {
        var order = Attempt();
        var payment = order.Payments.Single();
        payment.GatewayCreationAttempted = true;
        payment.GatewayOrderCode = 789123;
        var repository = DispatchProxy.Create<ICheckoutRepository, RepositoryProxy>();
        ((RepositoryProxy)(object)repository).Order = order;
        var gateway = new WebhookGateway();
        var realtime = new Realtime();
        var service = new CheckoutPaymentService(repository, gateway, new Inventory(), new Clock(),
            null!, NullLogger<CheckoutPaymentService>.Instance, new OrderConfirmationScheduling(new ConfirmationScheduler()),
            null!, new Notifications(), null!, new Audit(), null!, realtime);
        var body = SignedWebhook(789123, payment.Amount);
        await service.WebhookAsync(body);
        await service.WebhookAsync(body);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(0, gateway.Reads);
        Assert.Single(realtime.Events);
    }

    private sealed class Audit : IAuditWriter
    {
        public void Record(string type, Guid id, string action, object? oldValues = null, object? newValues = null, string? reason = null) { }
    }
    private sealed class ConfirmationScheduler : IShopOrderConfirmationScheduler
    {
        public string? Schedule(Guid id, DateTime deadline) => "confirmation-job";
        public bool Delete(string id) => true;
    }

    [Fact]
    public async Task InvalidWebhookCannotMarkRemainingPaymentPaid()
    {
        var (service, order, gateway, realtime) = WebhookFixture();
        var body = SignedWebhook(order.Payments.Last().GatewayOrderCode!.Value, 10000, "wrong-key");
        await Assert.ThrowsAsync<AppException>(() => service.WebhookAsync(body));
        Assert.Equal(PaymentStatus.Pending, order.Payments.Last().Status);
        Assert.Equal(0, gateway.Reads);
        Assert.Empty(realtime.Events);
    }

    private static (CheckoutPaymentService Service, Order Order, WebhookGateway Gateway, Realtime Realtime) WebhookFixture()
    {
        var order = Attempt();
        order.Status = OrderStatus.Confirmed;
        order.Payments.Single().Status = PaymentStatus.Succeeded;
        order.Payments.Single().OrderId = order.Id;
        order.Payments.Add(new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Amount = 10000,
            Status = PaymentStatus.Pending, PaymentType = PaymentType.Remaining, Method = PaymentMethod.OnlinePayment,
            GatewayCreationAttempted = true, GatewayOrderCode = 123456, GatewayCheckoutUrl = "https://pay.payos.vn/web/test", CreatedAt = Now });
        var repository = DispatchProxy.Create<ICheckoutRepository, RepositoryProxy>();
        ((RepositoryProxy)(object)repository).Order = order;
        var gateway = new WebhookGateway();
        var realtime = new Realtime();
        var service = new CheckoutPaymentService(repository, gateway, new Inventory(), new Clock(),
            null!, NullLogger<CheckoutPaymentService>.Instance, null!, null!, new Notifications(), null!, null!, null!, realtime);
        return (service, order, gateway, realtime);
    }

    private static JsonElement SignedWebhook(long orderCode, decimal amount, string key = "checksum")
    {
        var data = JsonSerializer.SerializeToElement(new { amount, code = "00", orderCode });
        var canonical = $"amount={amount}&code=00&orderCode={orderCode}";
        return JsonSerializer.SerializeToElement(new { code = "00", data, signature = PayOsCheckoutGateway.Sign(canonical, key) });
    }

    private sealed class WebhookGateway : ICheckoutPaymentGateway
    {
        public int Reads;
        public bool IsConfigured(ShopPaymentConfig? config) => true;
        public bool VerifyWebhook(JsonElement body, ShopPaymentConfig config) => PayOsCheckoutGateway.VerifyWebhookData(body, "checksum");
        public Task<GatewayPayment> GetAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct) { Reads++; throw new InvalidOperationException("A full signed webhook must not poll PayOS."); }
        public Task<GatewayPayment> CreateAsync(Order order, Payment payment, ShopPaymentConfig config, CancellationToken ct) => throw new NotSupportedException();
        public Task<GatewayPayment> CancelAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Realtime : IRealtimePublisher
    {
        public List<(string Type, JsonElement Data)> Events = [];
        public Task SendToUserAsync(Guid userId, string eventType, object data) { Events.Add((eventType, JsonSerializer.SerializeToElement(data))); return Task.CompletedTask; }
        public Task SendToAllAsync(string eventType, object data) => throw new NotSupportedException();
        public Task SendToGroupAsync(string groupName, string eventType, object data) => throw new NotSupportedException();
    }

    public class RepositoryProxy : DispatchProxy
    {
        public Order Order = null!;
        public int Restores;
        public int Releases;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            return method!.Name switch
            {
                "GetOrderAsync" or "GetOrderForHistoryAsync" => Task.FromResult<Order?>(Order),
                "FindGatewayOrderAsync" => Task.FromResult<Guid?>(Order.Id),
                "AddHistory" => null,
                "WithOrderLockAsync" => ((Func<Task<SaveBite.Backend.Models.Responses.CheckoutOrderResponse>>)args![1]!)(),
                "CompleteInventoryAsync" => Release(),
                "RestoreCartAsync" => Restore(),
                "SaveAsync" => Task.CompletedTask,
                _ => throw new NotSupportedException(method.Name)
            };
        }
        private Task Release() { if (Order.InventoryReservationActive) Releases++; Order.InventoryReservationActive = false; return Task.CompletedTask; }
        private Task Restore() { Restores++; return Task.CompletedTask; }
    }

    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Inventory : IOrderInventorySynchronizer { public Task SyncAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask; }
    private sealed class Notifications : INotificationQueue
    {
        public List<CreateNotificationCommand> Commands = [];
        public Task EnqueueAsync(NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default)
        { Commands.Add(command); return Task.CompletedTask; }
    }
}
