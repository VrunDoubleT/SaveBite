using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.IntegrationTests.Features.Payments;

public class PaymentIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public PaymentIntegrationTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    private async Task<(User customer, User owner, Shop shop, Order order, Payment payment)> SetupPaymentScenarioAsync(
        OrderStatus orderStatus = OrderStatus.AwaitingPayment,
        PaymentStatus paymentStatus = PaymentStatus.Pending,
        PaymentMethod paymentMethod = PaymentMethod.OnlinePayment,
        PaymentType paymentType = PaymentType.Full,
        decimal totalAmount = 50000,
        decimal depositAmount = 0,
        decimal remainingAmount = 0,
        bool createRemainingAttempt = false)
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@test.com",
            fullName: "Payment Shop Owner");

        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);

        var paymentConfig = new ShopPaymentConfig
        {
            ShopId = shop.Id,
            BankName = "Vietcombank",
            BankAccountNumber = "0123456789",
            BankAccountHolder = "Payment Shop Owner",
            PayosClientId = "payos-test-client-id",
            PayosApiKey = "payos-test-api-key",
            PayosChecksumKey = "payos-test-checksum-key",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Set<ShopPaymentConfig>().Add(paymentConfig);

        var customer = await AuthenticateAsCustomerAsync(
            email: $"customer_{Guid.NewGuid():N}@test.com",
            fullName: "Payment Test Customer");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = $"Category_{Guid.NewGuid():N}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Categories.Add(category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            CategoryId = category.Id,
            Name = "Bánh Mì Thanh Toán",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "PAY-SKU",
            OriginalPrice = 50000,
            DealPrice = 25000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        product.Variants.Add(variant);
        DbContext.Products.Add(product);

        var now = DateTime.UtcNow;
        var deal = new FlashDeal
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            ProductId = product.Id,
            SaleStartTime = now.AddHours(-2),
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(3),
            Status = FlashDealStatus.OnSale,
            CreatedAt = now,
            UpdatedAt = now
        };

        var dealVariant = new FlashDealVariant
        {
            Id = Guid.NewGuid(),
            FlashDealId = deal.Id,
            VariantId = variant.Id,
            OriginalPrice = 50000,
            DealPrice = 25000,
            DiscountPercent = 50,
            TotalQuantity = 20,
            ReservedQuantity = 10,
            SoldQuantity = 0,
            Status = FlashDealVariantStatus.Active
        };
        deal.Variants.Add(dealVariant);
        DbContext.FlashDeals.Add(deal);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = $"ORD{Random.Shared.Next(100000, 999999)}",
            CustomerId = customer.Id,
            ShopId = shop.Id,
            PaymentMethod = paymentMethod,
            Status = orderStatus,
            TotalAmount = totalAmount,
            DepositAmount = depositAmount,
            RemainingAmount = remainingAmount,
            RequiredOnlineAmount = paymentType == PaymentType.Deposit ? depositAmount : totalAmount,
            DistanceKm = 1.5m,
            EtaMinutes = 10,
            SoftDeadlineAt = now.AddHours(1),
            HardDeadlineAt = now.AddHours(2),
            CheckoutQuoteId = Guid.NewGuid(),
            PaymentExpiresAt = now.AddMinutes(15),
            InventoryReservationActive = orderStatus == OrderStatus.AwaitingPayment,
            ConfirmedAt = orderStatus is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup ? now.AddMinutes(-20) : null,
            CreatedAt = now.AddMinutes(-2),
            UpdatedAt = now
        };

        if (orderStatus != OrderStatus.AwaitingPayment)
        {
            order.StatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = OrderStatus.AwaitingPayment,
                ToStatus = OrderStatus.Pending,
                CreatedAt = now.AddMinutes(-10),
                Note = "Initial payment verified."
            });
        }
        if (orderStatus is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup)
        {
            order.StatusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = OrderStatus.Pending,
                ToStatus = OrderStatus.Confirmed,
                CreatedAt = now.AddMinutes(-5),
                Note = "Order confirmed."
            });
        }

        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FlashDealVariantId = dealVariant.Id,
            ProductNameSnapshot = product.Name,
            VariantNameSnapshot = variant.Sku,
            OriginalPriceSnapshot = variant.OriginalPrice,
            DealPriceSnapshot = variant.DealPrice,
            UnitPrice = variant.DealPrice,
            Quantity = 2,
            Subtotal = totalAmount
        };
        order.Items.Add(item);

        var paymentAmount = paymentType == PaymentType.Deposit ? depositAmount : totalAmount;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentType = paymentType,
            Method = PaymentMethod.OnlinePayment,
            Amount = paymentAmount,
            CollectedAmount = paymentStatus == PaymentStatus.Succeeded ? paymentAmount : 0,
            Status = paymentStatus,
            GatewayOrderCode = Random.Shared.NextInt64(100000000, 999999999),
            GatewayCreationAttempted = paymentStatus == PaymentStatus.Succeeded,
            PaidAt = paymentStatus == PaymentStatus.Succeeded ? now.AddMinutes(-2) : null,
            CreatedAt = now.AddMinutes(-2),
            UpdatedAt = now.AddMinutes(-2)
        };
        order.Payments.Add(payment);

        if (createRemainingAttempt)
        {
            var remainingPayment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentType = PaymentType.Remaining,
                Method = PaymentMethod.OnlinePayment,
                Amount = remainingAmount,
                CollectedAmount = 0,
                Status = PaymentStatus.Pending,
                GatewayOrderCode = Random.Shared.NextInt64(100000000, 999999999),
                GatewayCreationAttempted = false,
                CreatedAt = now.AddMinutes(-1),
                UpdatedAt = now.AddMinutes(-1)
            };
            order.Payments.Add(remainingPayment);
        }

        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();

        return (customer, owner, shop, order, payment);
    }

    [Fact]
    public async Task GetCustomerPayments_WhenPaymentsExist_ReturnsPagedPayments_200()
    {
        // Arrange
        var (customer, _, _, _, payment) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.Pending,
            paymentStatus: PaymentStatus.Succeeded);

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/payments?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CustomerPaymentResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.TotalItems.Should().BeGreaterThanOrEqualTo(1);
        result.Data.Items.Should().Contain(p => p.Id == payment.Id);
    }

    [Fact]
    public async Task GetCustomerPayments_WithStatusFilter_ReturnsFilteredPayments_200()
    {
        // Arrange
        var (customer, _, _, _, payment) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.Pending,
            paymentStatus: PaymentStatus.Succeeded);

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/payments?page=1&pageSize=10&status=Succeeded");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CustomerPaymentResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().OnlyContain(p => p.Status == "Succeeded");
    }

    [Fact]
    public async Task GetCustomerPaymentDetails_WhenOwnedPayment_ReturnsDetails_200()
    {
        // Arrange
        var (customer, _, _, order, payment) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.Pending,
            paymentStatus: PaymentStatus.Succeeded);

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync($"/api/payments/{payment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerPaymentDetailResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Payment.Id.Should().Be(payment.Id);
        result.Data.Order.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task GetCustomerPaymentDetails_WhenNotOwned_ReturnsNotFound_404()
    {
        // Arrange
        var (_, _, _, _, payment) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.Pending,
            paymentStatus: PaymentStatus.Succeeded);

        var otherCustomer = await AuthenticateAsCustomerAsync(
            email: $"other_customer_{Guid.NewGuid():N}@test.com",
            fullName: "Other Customer");

        SetBearerToken(otherCustomer.Id, otherCustomer.Email, otherCustomer.Role);

        // Act
        var response = await Client.GetAsync($"/api/payments/{payment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePaymentLink_PendingOrder_ReturnsPaymentLink_200()
    {
        // Arrange
        var (customer, _, _, order, payment) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.AwaitingPayment,
            paymentStatus: PaymentStatus.Pending,
            paymentMethod: PaymentMethod.OnlinePayment);

        Factory.CheckoutPaymentGatewayMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, Payment p, ShopPaymentConfig c, CancellationToken ct) =>
                new GatewayPayment("PENDING", p.GatewayOrderCode ?? 987654321, p.Amount, 0, "link_test_123", "https://pay.payos.vn/web/testlink999", "0002010102123854..."));

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.PostAsync($"/api/payments/orders/{order.Id}/payment-link", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutOrderResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.CheckoutUrl.Should().Be("https://pay.payos.vn/web/testlink999");

        // Verify database state updated
        var updatedPayment = await DbContext.Payments.AsNoTracking().SingleAsync(p => p.Id == payment.Id);
        updatedPayment.GatewayCreationAttempted.Should().BeTrue();
        updatedPayment.GatewayCheckoutUrl.Should().Be("https://pay.payos.vn/web/testlink999");
    }

    [Fact]
    public async Task GenerateRemainingPaymentQr_StoreOwner_EligibleOrder_ReturnsQr_200()
    {
        // Arrange: PayAtShop with confirmed status, deposit succeeded, remaining balance > 0
        var (customer, owner, shop, order, _) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.Confirmed,
            paymentStatus: PaymentStatus.Succeeded,
            paymentMethod: PaymentMethod.PayAtShop,
            paymentType: PaymentType.Deposit,
            totalAmount: 50000,
            depositAmount: 10000,
            remainingAmount: 40000);

        Factory.CheckoutPaymentGatewayMock
            .Setup(x => x.CreateAsync(It.IsAny<Order>(), It.IsAny<Payment>(), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, Payment p, ShopPaymentConfig c, CancellationToken ct) =>
                new GatewayPayment("PENDING", p.GatewayOrderCode ?? 777888999, p.Amount, 0, "rem_link_123", "https://pay.payos.vn/web/rem999", "0002010102123854rem..."));

        SetBearerToken(owner.Id, owner.Email, owner.Role);

        // Act
        var response = await Client.PostAsync($"/api/orders/shops/{shop.Id}/{order.Id}/remaining-payment", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Payments.Should().Contain(p => p.Type == "Remaining" && p.Status == "Pending" && p.CheckoutUrl != null);

        // Verify remaining payment was inserted into DB
        var remainingInDb = await DbContext.Payments.AsNoTracking()
            .SingleOrDefaultAsync(p => p.OrderId == order.Id && p.PaymentType == PaymentType.Remaining);
        remainingInDb.Should().NotBeNull();
        remainingInDb!.Amount.Should().Be(40000);
        remainingInDb.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task GenerateRemainingPaymentQr_WhenNonOwner_ReturnsForbidden_403()
    {
        // Arrange
        var (customer, _, shop, order, _) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.Confirmed,
            paymentStatus: PaymentStatus.Succeeded,
            paymentMethod: PaymentMethod.PayAtShop,
            paymentType: PaymentType.Deposit,
            totalAmount: 50000,
            depositAmount: 10000,
            remainingAmount: 40000);

        // Authenticate as customer instead of shop owner
        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.PostAsync($"/api/orders/shops/{shop.Id}/{order.Id}/remaining-payment", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateRemainingPaymentQr_WhenOrderNotEligible_ReturnsConflict_409()
    {
        // Arrange: OnlinePayment method is NOT eligible for remaining-payment QR
        var (_, owner, shop, order, _) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.Confirmed,
            paymentStatus: PaymentStatus.Succeeded,
            paymentMethod: PaymentMethod.OnlinePayment,
            paymentType: PaymentType.Full,
            totalAmount: 50000,
            depositAmount: 0,
            remainingAmount: 0);

        SetBearerToken(owner.Id, owner.Email, owner.Role);

        // Act
        var response = await Client.PostAsync($"/api/orders/shops/{shop.Id}/{order.Id}/remaining-payment", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReconcilePayment_WhenCustomerReconcilesPaidOrder_UpdatesStatusToPending_200()
    {
        // Arrange: AwaitingPayment order, pending payment
        var (customer, _, _, order, payment) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.AwaitingPayment,
            paymentStatus: PaymentStatus.Pending,
            paymentMethod: PaymentMethod.OnlinePayment,
            totalAmount: 50000);

        // Mark attempt so gateway can reconcile
        payment.GatewayCreationAttempted = true;
        await DbContext.SaveChangesAsync();

        // Mock gateway returning PAID
        Factory.CheckoutPaymentGatewayMock
            .Setup(x => x.GetAsync(It.Is<Payment>(p => p.Id == payment.Id), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, ShopPaymentConfig c, CancellationToken ct) =>
                new GatewayPayment("PAID", p.GatewayOrderCode ?? 123456, p.Amount, p.Amount, "link_paid", null));

        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.PostAsync($"/api/payments/orders/{order.Id}/reconcile", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutOrderResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Status.Should().Be(nameof(OrderStatus.Pending));
        result.Data.PaymentStatus.Should().Be(nameof(PaymentStatus.Succeeded));

        // Verify in DB
        var updatedOrder = await DbContext.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id);
        updatedOrder.Status.Should().Be(OrderStatus.Pending);

        var updatedPayment = await DbContext.Payments.AsNoTracking().SingleAsync(p => p.Id == payment.Id);
        updatedPayment.Status.Should().Be(PaymentStatus.Succeeded);
        updatedPayment.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PayOsWebhook_WhenValidSignature_UpdatesOrderAndPayment_200()
    {
        // Arrange: AwaitingPayment order, pending payment with distinct GatewayOrderCode
        var (customer, _, _, order, payment) = await SetupPaymentScenarioAsync(
            orderStatus: OrderStatus.AwaitingPayment,
            paymentStatus: PaymentStatus.Pending,
            paymentMethod: PaymentMethod.OnlinePayment,
            totalAmount: 50000);

        payment.GatewayCreationAttempted = true;
        var gatewayCode = payment.GatewayOrderCode!.Value;
        await DbContext.SaveChangesAsync();

        // Mock gateway to return PAID
        Factory.CheckoutPaymentGatewayMock
            .Setup(x => x.GetAsync(It.Is<Payment>(p => p.Id == payment.Id), It.IsAny<ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, ShopPaymentConfig c, CancellationToken ct) =>
                new GatewayPayment("PAID", gatewayCode, 50000, 50000, "link_paid_wh", null));

        // Webhook is anonymous
        Client.DefaultRequestHeaders.Authorization = null;

        var webhookPayload = new
        {
            code = "00",
            desc = "success",
            data = new
            {
                orderCode = gatewayCode,
                amount = 50000,
                description = "SB1234567",
                accountNumber = "0123456789",
                reference = "REF123456",
                transactionDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                currency = "VND",
                paymentLinkId = "link_paid_wh",
                code = "00",
                desc = "success"
            },
            signature = "valid_mock_signature"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/payments/payos/webhook", webhookPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in DB
        var updatedOrder = await DbContext.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id);
        updatedOrder.Status.Should().Be(OrderStatus.Pending);

        var updatedPayment = await DbContext.Payments.AsNoTracking().SingleAsync(p => p.Id == payment.Id);
        updatedPayment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public async Task PaymentEndpoints_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act & Assert 1: GET /api/payments
        var listResponse = await Client.GetAsync("/api/payments");
        listResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act & Assert 2: GET /api/payments/{id}
        var detailsResponse = await Client.GetAsync($"/api/payments/{Guid.NewGuid()}");
        detailsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act & Assert 3: POST /api/payments/orders/{id}/payment-link
        var createLinkResponse = await Client.PostAsync($"/api/payments/orders/{Guid.NewGuid()}/payment-link", null);
        createLinkResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act & Assert 4: POST /api/payments/orders/{id}/reconcile
        var reconcileResponse = await Client.PostAsync($"/api/payments/orders/{Guid.NewGuid()}/reconcile", null);
        reconcileResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
