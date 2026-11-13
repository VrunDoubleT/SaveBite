using System.Reflection;
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

    public class RepositoryProxy : DispatchProxy
    {
        public Order Order = null!;
        public int Restores;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            return method!.Name switch
            {
                "GetOrderAsync" or "GetOrderForHistoryAsync" => Task.FromResult<Order?>(Order),
                "WithOrderLockAsync" => ((Func<Task<SaveBite.Backend.Models.Responses.CheckoutOrderResponse>>)args![1]!)(),
                "CompleteInventoryAsync" => Release(),
                "RestoreCartAsync" => Restore(),
                "SaveAsync" => Task.CompletedTask,
                _ => throw new NotSupportedException(method.Name)
            };
        }
        private Task Release() { Order.InventoryReservationActive = false; return Task.CompletedTask; }
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
