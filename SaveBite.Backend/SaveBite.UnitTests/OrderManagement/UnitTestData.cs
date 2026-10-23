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

// Use deterministic time for checkout/payment boundaries without sleeping or contacting services.
internal sealed class UnitTestClock : TimeProvider
{
    public DateTime Now { get; set; } = DateTime.UtcNow;
    public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
}

internal static class UnitTestData
{
    public static User Customer() => new()
    {
        Id = Guid.NewGuid(), FullName = "Test Customer", Email = "customer@example.test", PasswordHash = "unused",
        Status = UserStatus.Active, CustomerStatus = CustomerStatus.Active, Role = UserRole.User
    };

    public static CartItem CartItem(Guid userId, DateTime now, decimal price = 50000, int quantity = 1)
    {
        var shop = new Shop { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Name = "Test Shop", Status = ShopStatus.Active,
            Latitude = 10.0294, Longitude = 105.7508, AddressLine = "Test address", PaymentConfig = new ShopPaymentConfig() };
        var product = new Product { Id = Guid.NewGuid(), ShopId = shop.Id, Shop = shop, Name = "Test Product", Status = ProductStatus.Active };
        var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, Product = product, IsActive = true, Sku = "Regular" };
        var deal = new FlashDeal { Id = Guid.NewGuid(), ShopId = shop.Id, Shop = shop, ProductId = product.Id, Product = product,
            Status = FlashDealStatus.OnSale, SaleStartTime = now.AddMinutes(-5), OrderEndTime = now.AddMinutes(90), ShopClosingTime = now.AddMinutes(100) };
        var dealVariant = new FlashDealVariant { Id = Guid.NewGuid(), FlashDealId = deal.Id, FlashDeal = deal, VariantId = variant.Id,
            Variant = variant, Status = FlashDealVariantStatus.Active, OriginalPrice = price + 10000, DealPrice = price, TotalQuantity = 10 };
        return new CartItem { Id = Guid.NewGuid(), UserId = userId, FlashDealVariantId = dealVariant.Id,
            FlashDealVariant = dealVariant, Quantity = quantity, AddedAt = now };
    }

    public static TrustLevel Level(DateTime now) => new()
    {
        Id = Guid.NewGuid(), Name = "Silver", Rank = 2, IsActive = true,
        DepositPolicy = new TrustLevelDepositPolicy
        {
            Id = Guid.NewGuid(), DepositFreeOrderAmount = 10000, MaxDepositOrderAmount = 100000, UpdatedAt = now,
            Ranges = new List<TrustLevelDepositRange>
            {
                new() { Id = Guid.NewGuid(), FromOrderAmount = 10000, ToOrderAmount = 40000, DepositAmount = 5000 },
                new() { Id = Guid.NewGuid(), FromOrderAmount = 40000, ToOrderAmount = 80000, DepositAmount = 20000 },
                new() { Id = Guid.NewGuid(), FromOrderAmount = 80000, ToOrderAmount = 100000, DepositAmount = 30000 }
            }
        }
    };

    public static Order Order(DateTime now, OrderStatus status = OrderStatus.Pending)
    {
        var customer = Customer(); var cart = CartItem(customer.Id, now);
        var order = new Order { Id = Guid.NewGuid(), OrderCode = "000123", ShopId = cart.FlashDealVariant.FlashDeal.ShopId,
            Shop = cart.FlashDealVariant.FlashDeal.Shop, CustomerId = customer.Id, Customer = customer,
            Status = status, PaymentMethod = PaymentMethod.PayAtShop, TotalAmount = 50000, DepositAmount = 5000,
            RequiredOnlineAmount = 5000, RemainingAmount = 45000, SoftDeadlineAt = now.AddMinutes(5), HardDeadlineAt = now.AddMinutes(15),
            PaymentExpiresAt = now.AddSeconds(120), CheckoutQuoteId = Guid.NewGuid(), CheckoutRequestId = Guid.NewGuid(),
            InventoryReservationActive = status == OrderStatus.AwaitingPayment, CreatedAt = now, UpdatedAt = now };
        order.Items.Add(new OrderItem { Id = Guid.NewGuid(), OrderId = order.Id, FlashDealVariantId = cart.FlashDealVariantId,
            FlashDealVariant = cart.FlashDealVariant, ProductNameSnapshot = "Purchased Product", VariantNameSnapshot = "Purchased Variant",
            UnitPrice = 25000, Quantity = 2, Subtotal = 50000 });
        var pending = status == OrderStatus.AwaitingPayment;
        order.Payments.Add(new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Order = order, PaymentType = PaymentType.Deposit,
            Method = PaymentMethod.OnlinePayment, Amount = 5000, Status = pending ? PaymentStatus.Pending : PaymentStatus.Succeeded,
            CollectedAmount = pending ? 0 : 5000, PaidAt = pending ? null : now, GatewayOrderCode = 123456789,
            CreatedAt = now, UpdatedAt = now });
        if (!pending) order.StatusHistories.Add(new OrderStatusHistory { Id = Guid.NewGuid(), OrderId = order.Id, ToStatus = OrderStatus.Pending, CreatedAt = now });
        return order;
    }

    public static Payment Remaining(Order order, DateTime now, PaymentStatus status = PaymentStatus.Pending)
    {
        var payment = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, Order = order, PaymentType = PaymentType.Remaining,
            Method = PaymentMethod.OnlinePayment, Status = status, Amount = 45000, GatewayOrderCode = 987654321,
            GatewayCreationAttempted = true, GatewayCheckoutUrl = status == PaymentStatus.Pending ? "https://pay.payos.vn/web/test" : null,
            CollectedAmount = status == PaymentStatus.Succeeded ? 45000 : 0, CreatedAt = now, UpdatedAt = now };
        order.Payments.Add(payment); return payment;
    }
}
