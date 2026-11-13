using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Caching.Interfaces;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Audit;
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

namespace SaveBite.Tests.OrderManagement.Support;

internal sealed class PaymentTestContext
{
    public UnitTestClock Clock { get; } = new();
    public Order Order { get; set; }
    public Mock<ICheckoutRepository> Repo { get; } = new();
    public Mock<ICheckoutPaymentGateway> Gateway { get; } = new();
    public Mock<IOrderInventorySynchronizer> Stock { get; } = new();
    public Mock<IShopOrderConfirmationScheduler> ConfirmationScheduler { get; } = new();
    public Mock<ICheckoutPaymentExpirationScheduler> Scheduler { get; } = new();
    public Mock<IOrderEventPublisher> Events { get; } = new();
    public Mock<IShopOrderEventService> Realtime { get; } = new();
    public CheckoutPaymentService Service { get; }
    public OrderService OrderService { get; }
    public string ProviderStatus { get; set; } = "PENDING";
    public decimal ProviderPaid { get; set; }
    public PaymentTestContext()
    {
        Order = UnitTestData.Order(Clock.Now, OrderStatus.AwaitingPayment);
        Repo.Setup(x => x.WithOrderLockAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, Func<Task<bool>> action, CancellationToken _) => action());
        Repo.Setup(x => x.WithOrderLockAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<CheckoutOrderResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, Func<Task<CheckoutOrderResponse>> action, CancellationToken _) => action());
        Repo.Setup(x => x.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Order);
        Repo.Setup(x => x.CompleteInventoryAsync(It.IsAny<Order>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<Order, bool, CancellationToken>((order, _, _) => order.InventoryReservationActive = false).Returns(Task.CompletedTask);
        Gateway.Setup(x => x.IsConfigured(It.IsAny<ShopPaymentConfig>())).Returns(true);
        Gateway.Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order _, Payment p, ShopPaymentConfig _, CancellationToken _) => Result(p));
        Gateway.Setup(x => x.GetAsync(It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, ShopPaymentConfig _, CancellationToken _) => Result(p));
        Gateway.Setup(x => x.CancelAsync(It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, ShopPaymentConfig _, CancellationToken _) => Result(p));
        Scheduler.Setup(x => x.Delete(It.IsAny<string>())).Returns(true);
        var db = TestDbContextFactory.Create();
        var resale = new OrderResaleService(db, Repo.Object, new Mock<IShopOrderRepository>().Object, new Mock<Hangfire.IBackgroundJobClient>().Object, Clock, new Mock<ILogger<OrderResaleService>>().Object);
        var confirmation = new OrderConfirmationScheduling(ConfirmationScheduler.Object);
        Service = new CheckoutPaymentService(Repo.Object, Gateway.Object, Stock.Object, Clock, Scheduler.Object,
            new Mock<ILogger<CheckoutPaymentService>>().Object, confirmation, Realtime.Object,
            new Mock<INotificationQueue>().Object, resale, new Mock<IAuditWriter>().Object, new Mock<IPayOsCredentialProtector>().Object);
        OrderService = new OrderService(Repo.Object, Service, Clock);
    }
    public Payment Initial => Order.Payments.First(x => x.PaymentType != PaymentType.Remaining);
    public GatewayPayment Result(Payment p) => new(ProviderStatus, p.GatewayOrderCode ?? 0, p.Amount, ProviderPaid,
        "provider-link", "https://pay.payos.vn/web/test", "verified-qr-data");
}
