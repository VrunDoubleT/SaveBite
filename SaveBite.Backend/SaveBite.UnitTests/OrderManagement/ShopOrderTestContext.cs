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

namespace SaveBite.Api.Tests.UnitTests.Support;

internal sealed class ShopOrderTestContext
{
    public UnitTestClock Clock { get; } = new();
    public Order Order { get; set; }
    public Mock<IShopOrderRepository> Shops { get; } = new();
    public Mock<ICheckoutRepository> Orders { get; } = new();
    public Mock<ICheckoutPaymentService> Payments { get; } = new();
    public Mock<ICheckoutPaymentGateway> Gateway { get; } = new();
    public Mock<ICheckoutStockCache> Stock { get; } = new();
    public Mock<IEmailService> Email { get; } = new();
    public Mock<IShopOrderConfirmationScheduler> Confirmation { get; } = new();
    public Mock<IShopOrderNoShowScheduler> NoShow { get; } = new();
    public Mock<IShopOrderEventService> Realtime { get; } = new();
    public Mock<IOrderEventPublisher> Events { get; } = new();
    public ShopOrderService Service { get; }
    public Guid? ActorOverride { get; set; }
    public Guid ActorId => ActorOverride ?? Order.Shop.OwnerUserId;
    public ShopOrderTestContext()
    {
        Order = UnitTestData.Order(Clock.Now);
        Shops.Setup(x => x.RequireAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Shops.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Order);
        Shops.Setup(x => x.ActorAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid user, Guid _, CancellationToken _) => new ShopOrderActor(user, "StoreOwner", "Shop Owner"));
        Shops.Setup(x => x.PageAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<DateOnly?>()))
            .ReturnsAsync(() => ((IReadOnlyList<Order>)new[] { Order }, 1, (IReadOnlyDictionary<string, int>)new Dictionary<string, int> { ["Pending"] = 1 }));
        Orders.Setup(x => x.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Order);
        Orders.Setup(x => x.WithOrderLockAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, Func<Task<bool>> action, CancellationToken _) => action());
        Orders.Setup(x => x.AddHistory(It.IsAny<OrderStatusHistory>())).Callback<OrderStatusHistory>(history => Order.StatusHistories.Add(history));
        Payments.Setup(x => x.ReconcileOrderAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, bool createLink, CancellationToken _) =>
            {
                if (createLink)
                {
                    var pending = Order.Payments.SingleOrDefault(x => x.PaymentType == PaymentType.Remaining && x.Status == PaymentStatus.Pending);
                    if (pending != null) { pending.GatewayCheckoutUrl = "https://pay.payos.vn/web/remaining"; pending.GatewayQrCode = "remaining-qr"; }
                }
                return CheckoutOrderMapper.Map(Order);
            });
        Gateway.Setup(x => x.IsConfigured(It.IsAny<ShopPaymentConfig>())).Returns(true);
        Gateway.Setup(x => x.GetAsync(It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, ShopPaymentConfig _, CancellationToken _) => new GatewayPayment("CANCELLED", p.GatewayOrderCode ?? 0, p.Amount, 0, null, null));
        Gateway.Setup(x => x.CancelAsync(It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, ShopPaymentConfig _, CancellationToken _) => new GatewayPayment("CANCELLED", p.GatewayOrderCode ?? 0, p.Amount, 0, null, null));
        Confirmation.Setup(x => x.Delete(It.IsAny<string>())).Returns(true);
        NoShow.Setup(x => x.Schedule(It.IsAny<Guid>(), It.IsAny<DateTime>())).Returns("no-show-job");
        Service = new ShopOrderService(Shops.Object, Orders.Object, Payments.Object, Gateway.Object, Stock.Object, Email.Object,
            Clock, new Mock<ILogger<ShopOrderService>>().Object, Confirmation.Object, Realtime.Object, NoShow.Object, Events.Object);
    }
    public Payment Initial => Order.Payments.First(x => x.PaymentType != PaymentType.Remaining);
    public void AsStaff()
    {
        ActorOverride = Guid.NewGuid();
        Shops.Setup(x => x.RequireAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Shops.Setup(x => x.ActorAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid user, Guid _, CancellationToken _) => new ShopOrderActor(user, "Staff", "Test Staff"));
    }
}
