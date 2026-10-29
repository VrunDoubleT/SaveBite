using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.IntegrationTests.Features.Orders;

public class OrderIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public OrderIntegrationTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    private async Task<(User customer, User owner, Shop shop, Order order)> SetupOrderScenarioAsync(
        OrderStatus status = OrderStatus.Pending)
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@test.com",
            fullName: "Order Shop Owner");

        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"customer_{Guid.NewGuid():N}@test.com",
            Phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            FullName = "Order Customer",
            PasswordHash = "dummy-hash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(customer);

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
            Name = "Bánh Mì Order Test",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "ORD-SKU",
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
            ReservedQuantity = 2,
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
            PaymentMethod = PaymentMethod.OnlinePayment,
            Status = status,
            TotalAmount = 50000,
            DepositAmount = 0,
            RemainingAmount = 0,
            DistanceKm = 2.5m,
            EtaMinutes = 15,
            SoftDeadlineAt = now.AddHours(1),
            HardDeadlineAt = now.AddHours(2),
            ConfirmedAt = status is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForPickup ? now.AddMinutes(-30) : null,
            ReadyAt = status == OrderStatus.ReadyForPickup ? now.AddMinutes(-10) : null,
            CreatedAt = now.AddMinutes(-2),
            UpdatedAt = now
        };

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
            Subtotal = 50000
        };
        order.Items.Add(item);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentType = PaymentType.Full,
            Method = PaymentMethod.OnlinePayment,
            Amount = order.TotalAmount,
            CollectedAmount = order.TotalAmount,
            Status = PaymentStatus.Succeeded,
            PaidAt = now.AddMinutes(-2),
            CreatedAt = now.AddMinutes(-2),
            UpdatedAt = now.AddMinutes(-2)
        };
        order.Payments.Add(payment);

        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();

        return (customer, owner, shop, order);
    }

    [Fact]
    public async Task GetCustomerOrders_WhenCustomerHasOrders_ReturnsOrdersList()
    {
        // Arrange
        var (customer, _, _, order) = await SetupOrderScenarioAsync();
        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync("/api/orders");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CheckoutOrderResponse>>>();

        // Assert
        _output.WriteLine($"[TEST LOG] GET /api/orders: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().ContainSingle(o => o.Id == order.Id);
    }

    [Fact]
    public async Task GetCustomerOrderById_WhenOwnedOrder_ReturnsOrderDetails()
    {
        // Arrange
        var (customer, _, _, order) = await SetupOrderScenarioAsync();
        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.GetAsync($"/api/orders/{order.Id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutOrderResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] GET /api/orders/{order.Id}: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(order.Id);
        result.Data.OrderCode.Should().Be(order.OrderCode);
    }

    [Fact]
    public async Task GetCustomerOrderById_WhenNotOwned_ReturnsNotFound()
    {
        // Arrange
        var (_, _, _, order) = await SetupOrderScenarioAsync();
        var anotherUser = await AuthenticateAsCustomerAsync(
            email: $"other_{Guid.NewGuid():N}@test.com",
            fullName: "Other Customer");

        // Act
        var response = await Client.GetAsync($"/api/orders/{order.Id}");

        // Assert
        _output.WriteLine($"[TEST LOG] GET /api/orders/{order.Id} (unowned): {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelOrder_WhenEligiblePendingOrder_ReturnsOkAndCancelled()
    {
        // Arrange
        var (customer, _, _, order) = await SetupOrderScenarioAsync(OrderStatus.Pending);
        SetBearerToken(customer.Id, customer.Email, customer.Role);

        // Act
        var response = await Client.PostAsync($"/api/orders/{order.Id}/cancellations", null);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutOrderResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] POST /api/orders/{order.Id}/cancellations: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        var orderInDb = await DbContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
        orderInDb.Should().NotBeNull();
        orderInDb!.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task GetShopOrders_WhenStoreOwner_ReturnsShopOrdersPage()
    {
        // Arrange
        var (_, owner, shop, order) = await SetupOrderScenarioAsync();
        SetBearerToken(owner.Id, owner.Email, owner.Role);

        // Act
        var response = await Client.GetAsync($"/api/orders/shops/{shop.Id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopOrderPageResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] GET /api/orders/shops/{shop.Id}: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Page.Items.Should().ContainSingle(o => o.Order.Id == order.Id);
    }

    [Fact]
    public async Task ConfirmOrder_WhenStoreOwner_PendingOrder_ReturnsConfirmed()
    {
        // Arrange
        var (_, owner, shop, order) = await SetupOrderScenarioAsync(OrderStatus.Pending);
        SetBearerToken(owner.Id, owner.Email, owner.Role);

        // Act
        var response = await Client.PostAsync($"/api/orders/shops/{shop.Id}/{order.Id}/confirm", null);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] POST /api/orders/shops/{shop.Id}/{order.Id}/confirm: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Order.Status.Should().Be("Confirmed");

        var orderInDb = await DbContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
        orderInDb!.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task RejectOrder_WhenStoreOwner_WithValidReason_ReturnsRejected()
    {
        // Arrange
        var (_, owner, shop, order) = await SetupOrderScenarioAsync(OrderStatus.Pending);
        SetBearerToken(owner.Id, owner.Email, owner.Role);
        var request = new RejectShopOrderRequest("Out of ingredients");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/orders/shops/{shop.Id}/{order.Id}/reject", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] POST /api/orders/shops/{shop.Id}/{order.Id}/reject: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Order.Status.Should().Be("Rejected");

        var orderInDb = await DbContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
        orderInDb!.Status.Should().Be(OrderStatus.Rejected);
        orderInDb.CancelReason.Should().Be("Out of ingredients");
    }

    [Fact]
    public async Task UpdatePreparationStatus_ConfirmedToPreparing_ReturnsPreparing()
    {
        // Arrange
        var (_, owner, shop, order) = await SetupOrderScenarioAsync(OrderStatus.Confirmed);
        SetBearerToken(owner.Id, owner.Email, owner.Role);
        var request = new PreparationStatusRequest("Preparing");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/orders/shops/{shop.Id}/{order.Id}/preparation", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] POST /api/orders/shops/{shop.Id}/{order.Id}/preparation (Preparing): {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Order.Status.Should().Be("Preparing");

        var orderInDb = await DbContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
        orderInDb!.Status.Should().Be(OrderStatus.Preparing);
    }

    [Fact]
    public async Task UpdatePreparationStatus_PreparingToReadyForPickup_ReturnsReadyForPickup()
    {
        // Arrange
        var (_, owner, shop, order) = await SetupOrderScenarioAsync(OrderStatus.Preparing);
        SetBearerToken(owner.Id, owner.Email, owner.Role);
        var request = new PreparationStatusRequest("ReadyForPickup");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/orders/shops/{shop.Id}/{order.Id}/preparation", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] POST /api/orders/shops/{shop.Id}/{order.Id}/preparation (ReadyForPickup): {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Order.Status.Should().Be("ReadyForPickup");

        var orderInDb = await DbContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
        orderInDb!.Status.Should().Be(OrderStatus.ReadyForPickup);
    }

    [Fact]
    public async Task CompleteOrder_WhenStoreOwner_ReadyForPickupOrder_ReturnsCompleted()
    {
        // Arrange
        var (_, owner, shop, order) = await SetupOrderScenarioAsync(OrderStatus.ReadyForPickup);
        SetBearerToken(owner.Id, owner.Email, owner.Role);
        var request = new CompleteShopOrderRequest(CashReceived: false);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/orders/shops/{shop.Id}/{order.Id}/complete", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShopOrderResponse>>();

        // Assert
        _output.WriteLine($"[TEST LOG] POST /api/orders/shops/{shop.Id}/{order.Id}/complete: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Order.Status.Should().Be("Completed");

        var orderInDb = await DbContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
        orderInDb!.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public async Task ShopOrderActions_WhenNonOwnerAttemptsConfirm_ReturnsForbidden_403()
    {
        // Arrange
        var (_, _, shop, order) = await SetupOrderScenarioAsync(OrderStatus.Pending);
        var anotherOwner = await AuthenticateAsStoreOwnerAsync(
            email: $"other_owner_{Guid.NewGuid():N}@test.com",
            fullName: "Other Store Owner");

        // Act
        var response = await Client.PostAsync($"/api/orders/shops/{shop.Id}/{order.Id}/confirm", null);

        // Assert
        _output.WriteLine($"[TEST LOG] POST /api/orders/shops/{shop.Id}/{order.Id}/confirm (Non-owner): {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
