using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.IntegrationTests.Features.StoreAnalytics;

/// <summary>
/// Integration tests for FE-17: Store Dashboard & Analytics (UC-97 .. UC-100, BR-113 .. BR-117).
/// Endpoints: /api/shops/me/analytics/*
/// </summary>
public class StoreAnalyticsIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public StoreAnalyticsIntegrationTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    private async Task<(User owner, Shop shop)> SetupShopWithCompletedOrderAsync()
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@test.com",
            fullName: "Analytics Shop Owner");

        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);

        var customer = await AuthenticateAsCustomerAsync(
            email: $"buyer_{Guid.NewGuid():N}@test.com",
            fullName: "Analytics Customer");

        // Re-authenticate as owner
        SetBearerToken(owner.Id, owner.Email, owner.Role);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderCode = $"ORD{Random.Shared.Next(100000, 999999)}",
            CustomerId = customer.Id,
            ShopId = shop.Id,
            PaymentMethod = PaymentMethod.OnlinePayment,
            Status = OrderStatus.Completed,
            TotalAmount = 200000m,
            DepositAmount = 200000m,
            RemainingAmount = 0m,
            RequiredOnlineAmount = 200000m,
            ConfirmedAt = DateTime.UtcNow.AddHours(-2),
            ReadyAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Orders.Add(order);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentType = PaymentType.Full,
            Method = PaymentMethod.OnlinePayment,
            Amount = 200000m,
            Status = PaymentStatus.Succeeded,
            PaidAt = DateTime.UtcNow.AddHours(-2),
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };
        DbContext.Payments.Add(payment);

        await DbContext.SaveChangesAsync();
        return (owner, shop);
    }

    [Fact]
    public async Task GetDashboard_ValidOwner_Returns200WithSnapshots()
    {
        // Arrange
        await SetupShopWithCompletedOrderAsync();

        // Act
        var response = await Client.GetAsync("/api/shops/me/analytics?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreDashboardAnalyticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.ShopName.Should().Be("Test Bakery Shop");
        body.Data.Selected.Should().NotBeNull();
        body.Data.Overall.Should().NotBeNull();

        _output.WriteLine($"[PASS] Store Dashboard loaded for {body.Data.ShopName}");
    }

    [Fact]
    public async Task GetRevenueOverview_ValidOwnerWithCompletedOrder_CalculatesRevenue_200()
    {
        // Arrange (UC-97, BR-114)
        await SetupShopWithCompletedOrderAsync();

        // Act
        var response = await Client.GetAsync("/api/shops/me/analytics/revenue-overview?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreRevenueOverviewResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.CompletedOrdersCount.Should().BeGreaterThanOrEqualTo(1);
        body.Data.ActuallyPaidCompletedAmount.Should().BeGreaterThanOrEqualTo(200000m);
        body.Data.SalesRevenue.Should().BeGreaterThanOrEqualTo(200000m);

        _output.WriteLine($"[PASS] Revenue Overview: {body.Data.SalesRevenue:N0} VND across {body.Data.CompletedOrdersCount} order(s)");
    }

    [Fact]
    public async Task GetOrderStatistics_ValidOwner_ReturnsOrderCounts_200()
    {
        // Arrange (UC-98, BR-115)
        await SetupShopWithCompletedOrderAsync();

        // Act
        var response = await Client.GetAsync("/api/shops/me/analytics/orders?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreOrderStatisticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TotalOrders.Should().BeGreaterThanOrEqualTo(1);
        body.Data.CompletedOrders.Should().BeGreaterThanOrEqualTo(1);
        body.Data.CompletionRatePercent.Should().BeGreaterThan(0m);

        _output.WriteLine($"[PASS] Order Statistics: {body.Data.TotalOrders} total, {body.Data.CompletionRatePercent}% completion rate");
    }

    [Fact]
    public async Task GetFlashDealStatistics_ValidOwner_ReturnsDeals_200()
    {
        // Arrange (UC-99, BR-116)
        await SetupShopWithCompletedOrderAsync();

        // Act
        var response = await Client.GetAsync("/api/shops/me/analytics/flash-deals?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StoreFlashDealStatisticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.SellThroughRatePercent.Should().BeGreaterThanOrEqualTo(0m);

        _output.WriteLine($"[PASS] Flash Deal Stats: Sell-through rate = {body.Data.SellThroughRatePercent}%");
    }

    [Fact]
    public async Task GetRevenueByPaymentMethod_ValidOwner_ReturnsBreakdown_200()
    {
        // Arrange (UC-100, BR-117)
        await SetupShopWithCompletedOrderAsync();

        // Act
        var response = await Client.GetAsync("/api/shops/me/analytics/payment-methods?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StorePaymentMethodRevenueResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TotalSucceededPaymentAmount.Should().BeGreaterThanOrEqualTo(200000m);
        body.Data.Methods.Should().Contain(m => m.PaymentMethod == PaymentMethod.OnlinePayment.ToString());

        _output.WriteLine($"[PASS] Payment Methods Breakdown: Total = {body.Data.TotalSucceededPaymentAmount:N0} VND");
    }

    [Fact]
    public async Task GetRevenueOverview_DateRangeInverted_Returns400BadRequest()
    {
        // Arrange
        await AuthenticateAsStoreOwnerAsync();

        // Act
        var response = await Client.GetAsync("/api/shops/me/analytics/revenue-overview?fromDate=2026-10-31&toDate=2026-10-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("[PASS] Inverted date range returned 400 BadRequest as expected.");
    }

    [Fact]
    public async Task GetRevenueOverview_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync("/api/shops/me/analytics/revenue-overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _output.WriteLine("[PASS] Missing token correctly blocked with 401 Unauthorized.");
    }

    [Fact]
    public async Task GetRevenueOverview_AsCustomer_Returns403Forbidden()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/shops/me/analytics/revenue-overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _output.WriteLine("[PASS] Customer access to store analytics correctly blocked with 403 Forbidden.");
    }
}
