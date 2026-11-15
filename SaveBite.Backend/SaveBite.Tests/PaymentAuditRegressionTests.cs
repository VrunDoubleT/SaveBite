using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Controllers;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

public class PaymentAuditRegressionTests
{
    [Fact]
    public void SuspendedOwnerCanReachFeePaymentToResolveDebt()
    {
        var method = typeof(ShopPlatformFeeController).GetMethod(nameof(ShopPlatformFeeController.PayPlatformFee))!;
        var authorization = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.StoreOwnerAnyStatus, authorization.Policy);
    }

    [Theory]
    [InlineData(2500.25, 2500)]
    [InlineData(2500.50, 2501)]
    [InlineData(2500.75, 2501)]
    [InlineData(0.49, 0)]
    [InlineData(0.50, 1)]
    public void FeeAndRefundTargetsUseWholeVnd(decimal amount, decimal expected)
        => Assert.Equal(expected, PlatformFeeAmounts.Round(amount));

    [Fact]
    public void PartialRefundKeepsWholeVndFeeTarget()
    {
        var order = new Order { TotalAmount = 10000, RefundedAmount = 5000 };
        var refund = new Refund { RefundType = RefundType.CustomerRequest, Status = RefundStatus.Transferred };
        var fee = new OrderPlatformFee { Id = Guid.NewGuid(), FeeAmount = 501 };
        var credit = RefundFeePolicy.Waive(order, refund, fee, DateTime.UtcNow);
        Assert.NotNull(credit);
        Assert.Equal(-250, credit.Amount); // 250.5 retained fee rounds to 251, not 250.50.
    }

    [Theory]
    [InlineData("CANCELLED")]
    [InlineData("EXPIRED")]
    [InlineData("NOT_FOUND")]
    public async Task CashReceiptPreservesClosedOnlineAttempt(string closure)
    {
        var (service, order, online, added) = Fixture(closure);
        await CollectCash(service, order);
        Assert.Equal(PaymentStatus.Cancelled, online.Status);
        Assert.Equal(PaymentMethod.OnlinePayment, online.Method);
        Assert.Equal(123456, online.GatewayOrderCode);
        Assert.Null(online.GatewayCheckoutUrl);
        var cash = Assert.Single(added.Payments);
        Assert.NotSame(online, cash);
        Assert.Equal(PaymentMethod.Cash, cash.Method);
        Assert.Equal(PaymentStatus.Succeeded, cash.Status);
        Assert.Equal(10000, cash.CollectedAmount);
        Assert.Null(cash.GatewayOrderCode);
        Assert.Equal(20000, order.Payments.Sum(CheckoutOrderMapper.Collected));
    }

    [Theory]
    [InlineData("PAID", 10000)]
    [InlineData("CANCELLED", 1000)]
    [InlineData("PENDING", 0)]
    public async Task UnresolvedOrCollectedOnlineMoneyBlocksCash(string closure, decimal collected)
    {
        var (service, order, online, added) = Fixture(closure, collected);
        await Assert.ThrowsAsync<AppException>(() => CollectCash(service, order));
        Assert.Empty(added.Payments);
        Assert.Equal(PaymentStatus.Pending, online.Status);
    }

    private static Task CollectCash(ShopOrderService service, Order order) =>
        (Task)typeof(ShopOrderService).GetMethod("CollectCashAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(service, [order, CancellationToken.None])!;

    private static (ShopOrderService, Order, Payment, ShopRepository) Fixture(string closure, decimal collected = 0)
    {
        var shops = DispatchProxy.Create<IShopOrderRepository, ShopRepository>();
        var added = (ShopRepository)(object)shops;
        var service = new ShopOrderService(shops, null!, null!, new Gateway(closure, collected), null!, null!,
            TimeProvider.System, NullLogger<ShopOrderService>.Instance, null!, null!, null!, null!, null!, null!, null!, null!);
        var online = new Payment { Id = Guid.NewGuid(), PaymentType = PaymentType.Remaining,
            Method = PaymentMethod.OnlinePayment, Status = PaymentStatus.Pending, Amount = 10000,
            GatewayCreationAttempted = true, GatewayOrderCode = 123456, GatewayCheckoutUrl = "https://pay.payos.vn/web/old" };
        var order = new Order { Id = Guid.NewGuid(), TotalAmount = 20000, RemainingAmount = 10000,
            PaymentMethod = PaymentMethod.PayAtShop, Shop = new Shop(), Payments = [
                new Payment { PaymentType = PaymentType.Deposit, Status = PaymentStatus.Succeeded, Amount = 10000 }, online] };
        return (service, order, online, added);
    }

    public class ShopRepository : DispatchProxy
    {
        public List<Payment> Payments = [];
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name != "AddPayment") throw new NotSupportedException(method.Name);
            Payments.Add((Payment)args![0]!);
            return null;
        }
    }

    private sealed class Gateway(string status, decimal collected) : ICheckoutPaymentGateway
    {
        public bool IsConfigured(ShopPaymentConfig? config) => true;
        public Task<GatewayPayment> CancelAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct)
            => Task.FromResult(new GatewayPayment(status, payment.GatewayOrderCode!.Value, payment.Amount, collected, null, null));
        public Task<GatewayPayment> GetAsync(Payment payment, ShopPaymentConfig config, CancellationToken ct) => throw new NotSupportedException();
        public Task<GatewayPayment> CreateAsync(Order order, Payment payment, ShopPaymentConfig config, CancellationToken ct) => throw new NotSupportedException();
        public bool VerifyWebhook(JsonElement body, ShopPaymentConfig config) => false;
    }
}
