using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreAnalytics;

/// <summary>
/// Unit Tests for Feature: View Revenue by Payment Method
/// Service: StoreAnalyticsService.GetRevenueByPaymentMethodAsync
/// Function Code: UT-125
/// Sheet Name: UT_ViewRevenueByPaymentMethod
/// </summary>
public class ViewRevenueByPaymentMethodTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IStoreAnalyticsRepository> _repositoryMock = new();
    private readonly StoreAnalyticsService _service;

    public static readonly Guid ValidOwnerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidShopId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public ViewRevenueByPaymentMethodTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new StoreAnalyticsService(_repositoryMock.Object, TimeProvider.System);
    }

    private Shop CreateValidShop() => new()
    {
        Id = ValidShopId,
        OwnerUserId = ValidOwnerUserId,
        Name = "Bánh Mì Ngon",
        Status = ShopStatus.Active
    };

    // =========================================================================
    // TCID01: Valid Owner, Payments Grouped by Method (BR-117) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetRevenueByPaymentMethod_ValidOwner_ReturnsMethodsBreakdown_Normal()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest { Preset = "today" };

        var paymentRows = new List<PaymentMethodAmountRow>
        {
            new(PaymentMethod.OnlinePayment, 5, 250000m),
            new(PaymentMethod.PayAtShop, 3, 150000m)
        };

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetSucceededPaymentsByMethodAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentRows);

        // Act
        var result = await _service.GetRevenueByPaymentMethodAsync(ValidOwnerUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.TotalSucceededPaymentAmount.Should().Be(400000m);
        result.TotalSucceededPaymentCount.Should().Be(8);
        result.Methods.Should().NotBeEmpty();

        var online = result.Methods.First(m => m.PaymentMethod == PaymentMethod.OnlinePayment.ToString());
        online.TotalAmount.Should().Be(250000m);
        online.TransactionCount.Should().Be(5);
        online.SharePercent.Should().Be(62.5m); // 250k / 400k = 62.5%

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Total: {result.TotalSucceededPaymentAmount:N0} VND across {result.TotalSucceededPaymentCount} payments.");
    }

    // =========================================================================
    // TCID02: User has no Shop -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetRevenueByPaymentMethod_NoOwnedShop_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new AnalyticsPeriodRequest();
        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var act = () => _service.GetRevenueByPaymentMethodAsync(ValidOwnerUserId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _output.WriteLine($"[TCID02 PASSED] 404 NotFound thrown as expected: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: Zero Payments in Period -> Returns 0 Amounts and 0% Percentages (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetRevenueByPaymentMethod_ZeroPayments_ReturnsZeroAmounts_Boundary()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new AnalyticsPeriodRequest { Preset = "yesterday" };

        _repositoryMock
            .Setup(r => r.GetOwnedShopAsync(ValidOwnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetSucceededPaymentsByMethodAsync(ValidShopId, It.IsAny<AnalyticsPeriod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentMethodAmountRow>());

        // Act
        var result = await _service.GetRevenueByPaymentMethodAsync(ValidOwnerUserId, request);

        // Assert
        result.TotalSucceededPaymentAmount.Should().Be(0m);
        result.TotalSucceededPaymentCount.Should().Be(0);
        result.Methods.Should().AllSatisfy(m => m.SharePercent.Should().Be(0m));

        _output.WriteLine("[TCID03 PASSED] 0 payments cleanly handled with 0% across all payment methods.");
    }
}
