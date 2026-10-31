using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.IntegrationTests.Features.PlatformAnalytics;

/// <summary>
/// Integration tests for FE-23: Platform Dashboard & Analytics (UC-139 .. UC-144, BR-184 .. BR-191).
/// Endpoints: /api/admin/analytics/*
/// </summary>
public class PlatformAnalyticsIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public PlatformAnalyticsIntegrationTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task GetDashboard_AsAdmin_ReturnsExecutiveSnapshots_200()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/analytics?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformDashboardAnalyticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Selected.Should().NotBeNull();
        body.Data.Overall.Should().NotBeNull();

        _output.WriteLine("[PASS] Platform Executive Dashboard loaded successfully with both snapshots.");
    }

    [Fact]
    public async Task GetPlatformFeeStatistics_AsAdmin_ReturnsFeeMetrics_200()
    {
        var admin = await AuthenticateAsAdminAsync();

        var feeConfig = new PlatformFeeConfig
        {
            Id = Guid.NewGuid(),
            FeeRate = 0.08m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-10),
            CreatedBy = admin.Id,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.PlatformFeeConfigs.Add(feeConfig);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/analytics/platform-fees?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformFeeStatisticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.CurrentFeeRatePercent.Should().Be(8.0m);

        _output.WriteLine($"[PASS] Platform Fee Stats: Current fee rate = {body.Data.CurrentFeeRatePercent}%");
    }

    [Fact]
    public async Task GetUserStatistics_AsAdmin_ReturnsUserGrowthAndRoles_200()
    {
        // Arrange (UC-140, BR-185)
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/analytics/users?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformUserStatisticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TotalUsers.Should().BeGreaterThanOrEqualTo(1);
        body.Data.RoleBreakdown.Should().NotBeEmpty();

        _output.WriteLine($"[PASS] User Stats: Total users = {body.Data.TotalUsers}");
    }

    [Fact]
    public async Task GetShopStatistics_AsAdmin_ReturnsShopCounts_200()
    {
        // Arrange (UC-141, BR-186)
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/analytics/shops?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformShopStatisticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.ShopStatusBreakdown.Should().NotBeNull();

        _output.WriteLine($"[PASS] Shop Stats: Total shops = {body.Data.TotalShops}");
    }

    [Fact]
    public async Task GetRevenueAndOrderStatistics_AsAdmin_ReturnsPlatformFinancials_200()
    {
        // Arrange (UC-142, BR-187, BR-188)
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/analytics/revenue-orders?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformRevenueAndOrderStatisticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.OrderCompletionRatePercent.Should().BeGreaterThanOrEqualTo(0m);

        _output.WriteLine($"[PASS] Platform Revenue & Orders: Total orders = {body.Data.TotalOrdersInPeriod}");
    }

    [Fact]
    public async Task GetFlashDealStatistics_AsAdmin_ReturnsImpactMetrics_200()
    {
        // Arrange (UC-143, BR-189, BR-190)
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/analytics/flash-deals?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformFlashDealStatisticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.SellThroughRatePercent.Should().BeGreaterThanOrEqualTo(0m);

        _output.WriteLine($"[PASS] Platform Flash Deals: Sell-through rate = {body.Data.SellThroughRatePercent}%");
    }

    [Fact]
    public async Task GetPaymentAndRefundStatistics_AsAdmin_ReturnsGatewayMetrics_200()
    {
        // Arrange (UC-144, BR-191)
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/analytics/payments-refunds?preset=today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformPaymentAndRefundStatisticsResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TotalPaymentTransactions.Should().BeGreaterThanOrEqualTo(0);

        _output.WriteLine($"[PASS] Payment & Refund Stats: Total txns = {body.Data.TotalPaymentTransactions}");
    }

    [Fact]
    public async Task GetAnalytics_AsCustomer_Returns403Forbidden()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/analytics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _output.WriteLine("[PASS] Non-admin access to admin analytics correctly blocked with 403 Forbidden.");
    }
}
