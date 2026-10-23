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

namespace SaveBite.Api.Tests.UnitTests;

[Trait("Category", "Unit")]
public class CheckoutOrderTests
{
    private readonly ITestOutputHelper _output;
    private readonly CheckoutTestContext _context;
    private readonly CheckoutService _service;
    private readonly Mock<ICheckoutRepository> _checkoutRepoMock;
    private readonly Mock<ICheckoutQuoteStore> _quoteStoreMock;
    private readonly Mock<ICheckoutPaymentService> _paymentMock;

    public CheckoutOrderTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new CheckoutTestContext();
        _service = _context.Service;
        _checkoutRepoMock = _context.Repo;
        _quoteStoreMock = _context.Quotes;
        _paymentMock = _context.Payments;
    }

    // TCID01_CheckoutOrder_Context_ShowsLevelRangeAndDoesNotReserve_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_CheckoutOrder_Context_ShowsLevelRangeAndDoesNotReserve_Normal()
    {
        // Arrange
        var userId = _context.User.Id;

        // Act
        var result = await _service.ContextAsync(userId, _context.ShopId);

        // Assert
        result.Policy.LevelRank.Should().Be(2);
        result.Policy.RequiredDeposit.Should().Be(20000m);
        result.Policy.AppliedRange!.FromOrderAmount.Should().Be(40000m);
        _checkoutRepoMock.Verify(r => r.ReserveAsync(It.IsAny<IReadOnlyList<CartItem>>(), It.IsAny<CancellationToken>()), Times.Never);
        _quoteStoreMock.Verify(r => r.SaveAsync(It.IsAny<CheckoutQuote>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID01 PASSED: CheckoutOrder_Context_ShowsLevelRangeAndDoesNotReserve (Normal).");
    }

    // TCID02_CheckoutOrder_Quote_CachesAgreedGpsAndDeadlines_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID02_CheckoutOrder_Quote_CachesAgreedGpsAndDeadlines_Normal()
    {
        // Arrange
        var request = new CheckoutQuoteRequest(_context.ShopId, _context.Location);

        // Act
        var result = await _service.QuoteAsync(_context.User.Id, request);

        // Assert
        result.Shop.Subtotal.Should().Be(50000m);
        result.Pickup.SoftDeadlineAt.Should().Be(_context.Clock.Now.AddMinutes(5));
        result.Pickup.HardDeadlineAt.Should().Be(_context.Clock.Now.AddMinutes(15));
        result.ExpiresAt.Should().Be(_context.Clock.Now.AddSeconds(60));
        _context.Quote!.Location.Should().Be(request.Location);
        _context.Quote.Fingerprint.Should().NotBeNullOrWhiteSpace();
        _quoteStoreMock.Verify(r => r.SaveAsync(It.Is<CheckoutQuote>(q => q.UserId == _context.User.Id && q.TotalAmount == 50000), It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID02 PASSED: CheckoutOrder_Quote_CachesAgreedGpsAndDeadlines (Normal).");
    }

    // TCID03_CheckoutOrder_OnlinePayment_ReservesFullAmountFor120Seconds_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID03_CheckoutOrder_OnlinePayment_ReservesFullAmountFor120Seconds_Normal()
    {
        // Arrange
        var request = await _context.Request("OnlinePayment");

        // Act
        var result = await _service.PlaceAsync(_context.User.Id, request);

        // Assert
        result.Status.Should().Be("AwaitingPayment");
        result.RequiredOnlineAmount.Should().Be(50000m);
        result.PaymentExpiresAt.Should().Be(_context.Clock.Now.AddSeconds(120));
        _context.CreatedOrder!.OrderCode.Should().Be("000124");
        _context.CreatedOrder.Items.Single().UnitPrice.Should().Be(50000m);
        _checkoutRepoMock.Verify(r => r.ReserveAsync(_context.Items, It.IsAny<CancellationToken>()), Times.Once);
        _checkoutRepoMock.Verify(r => r.RemoveShopCartAsync(_context.User.Id, _context.ShopId, It.IsAny<CancellationToken>()), Times.Once);
        _context.Scheduler.Verify(r => r.Schedule(_context.CreatedOrder.Id, _context.Clock.Now.AddSeconds(120)), Times.Once);
        _paymentMock.Verify(r => r.StartAsync(_context.User.Id, _context.CreatedOrder.Id, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID03 PASSED: CheckoutOrder_OnlinePayment_ReservesFullAmountFor120Seconds (Normal).");
    }

    // TCID04_CheckoutOrder_PayAtShop_UsesFixedDepositAndRecordedBalance_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID04_CheckoutOrder_PayAtShop_UsesFixedDepositAndRecordedBalance_Normal()
    {
        // Arrange
        var request = await _context.Request();

        // Act
        var result = await _service.PlaceAsync(_context.User.Id, request);

        // Assert
        result.RequiredOnlineAmount.Should().Be(20000m);
        result.RemainingAtShop.Should().Be(30000m);
        _context.CreatedOrder!.Payments.Single().PaymentType.Should().Be(PaymentType.Deposit);
        _context.CreatedOrder.DepositAmount.Should().Be(20000m);
        _output.WriteLine("TCID04 PASSED: CheckoutOrder_PayAtShop_UsesFixedDepositAndRecordedBalance (Normal).");
    }

    // TCID05_CheckoutOrder_FreeDeposit_BecomesPendingWithoutExpiryJob_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_CheckoutOrder_FreeDeposit_BecomesPendingWithoutExpiryJob_Boundary()
    {
        // Arrange
        _context.Item.FlashDealVariant.DealPrice = 10000;
        var request = await _context.Request();

        // Act
        var result = await _service.PlaceAsync(_context.User.Id, request);

        // Assert
        result.Status.Should().Be("Pending"); result.RequiredOnlineAmount.Should().Be(0);
        result.RemainingAtShop.Should().Be(10000m);
        _context.Scheduler.Verify(r => r.Schedule(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
        _context.Events.Verify(r => r.QueuePendingAsync(_context.CreatedOrder!, It.IsAny<CancellationToken>()), Times.Once);
        _checkoutRepoMock.Verify(r => r.CompleteInventoryAsync(_context.CreatedOrder!, true, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID05 PASSED: CheckoutOrder_FreeDeposit_BecomesPendingWithoutExpiryJob (Boundary).");
    }

    // TCID06_CheckoutOrder_RepeatedRequest_ReusesOrderEvenAfterQuoteLoss_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID06_CheckoutOrder_RepeatedRequest_ReusesOrderEvenAfterQuoteLoss_Boundary()
    {
        // Arrange
        var request = await _context.Request();
        var first = await _service.PlaceAsync(_context.User.Id, request);
        _context.Quote = null;

        // Act
        var second = await _service.PlaceAsync(_context.User.Id, request);

        // Assert
        second.Id.Should().Be(first.Id);
        _checkoutRepoMock.Verify(r => r.AddOrder(It.IsAny<Order>()), Times.Once);
        _checkoutRepoMock.Verify(r => r.ReserveAsync(It.IsAny<IReadOnlyList<CartItem>>(), It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID06 PASSED: CheckoutOrder_RepeatedRequest_ReusesOrderEvenAfterQuoteLoss (Boundary).");
    }

    // TCID07_CheckoutOrder_UsedRequestWithDifferentMethod_ThrowsConflict_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID07_CheckoutOrder_UsedRequestWithDifferentMethod_ThrowsConflict_Abnormal()
    {
        // Arrange
        var request = await _context.Request();
        await _service.PlaceAsync(_context.User.Id, request);

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request with { PaymentMethod = "OnlinePayment" });

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _checkoutRepoMock.Verify(r => r.AddOrder(It.IsAny<Order>()), Times.Once);
        _output.WriteLine("TCID07 PASSED: CheckoutOrder_UsedRequestWithDifferentMethod_ThrowsConflict (Abnormal).");
    }

    // TCID08_CheckoutOrder_InvalidPaymentMethod_ThrowsBadRequest_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("Cash")]
    [InlineData("")]
    [InlineData("PayLater")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID08_CheckoutOrder_InvalidPaymentMethod_ThrowsBadRequest_Abnormal(string method)
    {
        // Arrange
        var request = new PlaceCheckoutOrderRequest(Guid.NewGuid(), method, Guid.NewGuid());

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _checkoutRepoMock.Verify(r => r.AddOrder(It.IsAny<Order>()), Times.Never);
        _output.WriteLine("TCID08 PASSED: CheckoutOrder_InvalidPaymentMethod_ThrowsBadRequest (Abnormal).");
    }

    // TCID09_CheckoutOrder_MissingQuote_RejectsWithoutStockReservation_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID09_CheckoutOrder_MissingQuote_RejectsWithoutStockReservation_Abnormal()
    {
        // Arrange
        var request = new PlaceCheckoutOrderRequest(Guid.NewGuid(), "OnlinePayment", Guid.NewGuid());

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _checkoutRepoMock.Verify(r => r.ReserveAsync(It.IsAny<IReadOnlyList<CartItem>>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID09 PASSED: CheckoutOrder_MissingQuote_RejectsWithoutStockReservation (Abnormal).");
    }

    // TCID10_CheckoutOrder_QuoteAtExactExpiry_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID10_CheckoutOrder_QuoteAtExactExpiry_Rejects_Boundary()
    {
        // Arrange
        var request = await _context.Request(); _context.Clock.Now = _context.Quote!.ExpiresAt;

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _checkoutRepoMock.Verify(r => r.AddOrder(It.IsAny<Order>()), Times.Never);
        _output.WriteLine("TCID10 PASSED: CheckoutOrder_QuoteAtExactExpiry_Rejects (Boundary).");
    }

    // TCID11_CheckoutOrder_StockSoldAfterQuote_RejectsFreshDbState_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID11_CheckoutOrder_StockSoldAfterQuote_RejectsFreshDbState_Abnormal()
    {
        // Arrange
        var request = await _context.Request(); _context.Item.FlashDealVariant.SoldQuantity = 10;

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _checkoutRepoMock.Verify(r => r.AddOrder(It.IsAny<Order>()), Times.Never);
        _paymentMock.Verify(r => r.StartAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID11 PASSED: CheckoutOrder_StockSoldAfterQuote_RejectsFreshDbState (Abnormal).");
    }

    // TCID12_CheckoutOrder_QuoteInputsChanged_RequiresNewQuote_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("quantity")]
    [InlineData("price")]
    [InlineData("deposit")]
    [InlineData("location")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID12_CheckoutOrder_QuoteInputsChanged_RequiresNewQuote_Abnormal(string change)
    {
        // Arrange
        var request = await _context.Request();
        switch (change)
        {
            case "quantity": _context.Item.Quantity = 2; break;
            case "price": _context.Item.FlashDealVariant.DealPrice = 55000; break;
            case "deposit": _context.Level.DepositPolicy!.Ranges.Single(r => r.FromOrderAmount == 40000).DepositAmount = 15000; break;
            case "location": _context.Item.FlashDealVariant.FlashDeal.Shop.Latitude += 0.1; break;
        }

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _checkoutRepoMock.Verify(r => r.AddOrder(It.IsAny<Order>()), Times.Never);
        _output.WriteLine("TCID12 PASSED: CheckoutOrder_QuoteInputsChanged_RequiresNewQuote (Abnormal).");
    }

    // TCID13_CheckoutOrder_OnlineOnlyRestrictions_BlockPayAtShop_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("level-one")]
    [InlineData("permanent-lock")]
    [InlineData("recovering")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID13_CheckoutOrder_OnlineOnlyRestrictions_BlockPayAtShop_Abnormal(string restriction)
    {
        // Arrange
        if (restriction == "level-one") { _context.Level.Rank = 1; _context.Level.DepositPolicy!.RequireFullOnlinePayment = true; }
        if (restriction == "permanent-lock") _context.PermanentLock = true;
        if (restriction == "recovering") _context.Recovering = true;
        var request = await _context.Request();

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _checkoutRepoMock.Verify(r => r.ReserveAsync(It.IsAny<IReadOnlyList<CartItem>>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID13 PASSED: CheckoutOrder_OnlineOnlyRestrictions_BlockPayAtShop (Abnormal).");
    }

    // TCID14_CheckoutOrder_ShopMembershipChangedAfterQuote_PropagatesForbidden_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID14_CheckoutOrder_ShopMembershipChangedAfterQuote_PropagatesForbidden_Abnormal()
    {
        // Arrange
        var request = await _context.Request();
        _checkoutRepoMock.Setup(r => r.GetStateAsync(_context.User.Id, _context.ShopId, It.IsAny<CancellationToken>())).ThrowsAsync(AppException.Forbidden("Own shop purchase is prohibited."));

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _checkoutRepoMock.Verify(r => r.AddOrder(It.IsAny<Order>()), Times.Never);
        _output.WriteLine("TCID14 PASSED: CheckoutOrder_ShopMembershipChangedAfterQuote_PropagatesForbidden (Abnormal).");
    }

    // TCID15_CheckoutOrder_GatewayUnavailable_DoesNotCreateOrder_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID15_CheckoutOrder_GatewayUnavailable_DoesNotCreateOrder_Abnormal()
    {
        // Arrange
        var request = await _context.Request();
        _context.Gateway.Setup(r => r.IsConfigured(It.IsAny<ShopPaymentConfig>())).Returns(false);

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        _checkoutRepoMock.Verify(r => r.ReserveAsync(It.IsAny<IReadOnlyList<CartItem>>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID15 PASSED: CheckoutOrder_GatewayUnavailable_DoesNotCreateOrder (Abnormal).");
    }

    // TCID16_CheckoutOrder_ReserveConflict_DoesNotStartPayment_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID16_CheckoutOrder_ReserveConflict_DoesNotStartPayment_Abnormal()
    {
        // Arrange
        var request = await _context.Request();
        _checkoutRepoMock.Setup(r => r.ReserveAsync(It.IsAny<IReadOnlyList<CartItem>>(), It.IsAny<CancellationToken>())).ThrowsAsync(AppException.OutOfStock());

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _checkoutRepoMock.Verify(r => r.AddOrder(It.IsAny<Order>()), Times.Never);
        _paymentMock.Verify(r => r.StartAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID16 PASSED: CheckoutOrder_ReserveConflict_DoesNotStartPayment (Abnormal).");
    }

    // TCID17_CheckoutOrder_MissingGps_RejectsBeforeRouting_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID17_CheckoutOrder_MissingGps_RejectsBeforeRouting_Abnormal()
    {
        // Arrange
        var request = new CheckoutQuoteRequest(_context.ShopId, null!);

        // Act
        Func<Task> act = () => _service.QuoteAsync(_context.User.Id, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _context.Travel.Verify(r => r.EstimateAsync(It.IsAny<CheckoutLocation>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID17 PASSED: CheckoutOrder_MissingGps_RejectsBeforeRouting (Abnormal).");
    }

    // TCID18_CheckoutOrder_InvalidGps_RejectsBeforeQuoteStorage_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(91, 10, 0)]
    [InlineData(10, 1001, 0)]
    [InlineData(10, -1, 0)]
    [InlineData(10, 10, 121)]
    [InlineData(10, 10, -31)]
    [InlineData(double.NaN, 10, 0)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID18_CheckoutOrder_InvalidGps_RejectsBeforeQuoteStorage_Boundary(double latitude, double accuracy, int ageSeconds)
    {
        // Arrange
        var location = new CheckoutLocation(latitude, 105, accuracy, _context.Clock.Now.AddSeconds(-ageSeconds));

        // Act
        Func<Task> act = () => _service.QuoteAsync(_context.User.Id, new(_context.ShopId, location));

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _quoteStoreMock.Verify(r => r.SaveAsync(It.IsAny<CheckoutQuote>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID18 PASSED: CheckoutOrder_InvalidGps_RejectsBeforeQuoteStorage (Boundary).");
    }

    // TCID19_CheckoutOrder_MixedShopCart_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID19_CheckoutOrder_MixedShopCart_Rejects_Abnormal()
    {
        // Arrange
        _context.Items.Add(UnitTestData.CartItem(_context.User.Id, _context.Clock.Now));

        // Act
        Func<Task> act = () => _service.ContextAsync(_context.User.Id, _context.ShopId);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("TCID19 PASSED: CheckoutOrder_MixedShopCart_Rejects (Abnormal).");
    }

    // TCID20_CheckoutOrder_EmptyCart_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID20_CheckoutOrder_EmptyCart_Rejects_Boundary()
    {
        // Arrange
        _context.Items.Clear();

        // Act
        Func<Task> act = () => _service.ContextAsync(_context.User.Id, _context.ShopId);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _output.WriteLine("TCID20 PASSED: CheckoutOrder_EmptyCart_Rejects (Boundary).");
    }

    // TCID21_CheckoutOrder_DepositIntervalBoundaries_SelectExpectedFixedDeposit_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(10000, 0, true)]
    [InlineData(10001, 5000, true)]
    [InlineData(40000, 5000, true)]
    [InlineData(40001, 20000, true)]
    [InlineData(80000, 20000, true)]
    [InlineData(80001, 30000, true)]
    [InlineData(100000, 30000, true)]
    [InlineData(100001, 0, false)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID21_CheckoutOrder_DepositIntervalBoundaries_SelectExpectedFixedDeposit_Boundary(int amount, int expectedDeposit, bool allowed)
    {
        // Arrange
        _context.Item.FlashDealVariant.DealPrice = amount;

        // Act
        var result = await _service.ContextAsync(_context.User.Id, _context.ShopId);

        // Assert
        result.Policy.RequiredDeposit.Should().Be(expectedDeposit);
        result.Policy.PayAtShopAllowed.Should().Be(allowed);
        _output.WriteLine("TCID21 PASSED: CheckoutOrder_DepositIntervalBoundaries_SelectExpectedFixedDeposit (Boundary).");
    }

    // TCID22_CheckoutOrder_DealWindowBoundary_RejectsAtOrOutsideSale_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("start-future")]
    [InlineData("order-end")]
    [InlineData("shop-close")]
    [Trait("CaseType", "Boundary")]
    public async Task TCID22_CheckoutOrder_DealWindowBoundary_RejectsAtOrOutsideSale_Boundary(string boundary)
    {
        // Arrange
        var deal = _context.Item.FlashDealVariant.FlashDeal;
        if (boundary == "start-future") deal.SaleStartTime = _context.Clock.Now.AddTicks(1);
        if (boundary == "order-end") deal.OrderEndTime = _context.Clock.Now;
        if (boundary == "shop-close") deal.ShopClosingTime = _context.Clock.Now;

        // Act
        Func<Task> act = () => _service.QuoteAsync(_context.User.Id, new(_context.ShopId, _context.Location));

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _output.WriteLine("TCID22 PASSED: CheckoutOrder_DealWindowBoundary_RejectsAtOrOutsideSale (Boundary).");
    }

    // TCID23_CheckoutOrder_LongTravel_AddsThirtyPercentBufferAndCapsToDealEnd_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID23_CheckoutOrder_LongTravel_AddsThirtyPercentBufferAndCapsToDealEnd_Boundary()
    {
        // Arrange
        _context.Travel.Setup(r => r.EstimateAsync(It.IsAny<CheckoutLocation>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TravelEstimate(20, 40));
        _context.Item.FlashDealVariant.FlashDeal.OrderEndTime = _context.Clock.Now.AddMinutes(50);

        // Act
        var result = await _service.QuoteAsync(_context.User.Id, new(_context.ShopId, _context.Location));

        // Assert
        result.Pickup.SoftDeadlineAt.Should().Be(_context.Clock.Now.AddMinutes(40));
        result.Pickup.BufferMinutes.Should().Be(12);
        result.Pickup.HardDeadlineAt.Should().Be(_context.Clock.Now.AddMinutes(50));
        result.Pickup.BufferWasCapped.Should().BeTrue();
        _output.WriteLine("TCID23 PASSED: CheckoutOrder_LongTravel_AddsThirtyPercentBufferAndCapsToDealEnd (Boundary).");
    }

    // TCID24_CheckoutOrder_InsufficientPickupWindow_Rejects_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID24_CheckoutOrder_InsufficientPickupWindow_Rejects_Boundary()
    {
        // Arrange
        _context.Item.FlashDealVariant.FlashDeal.OrderEndTime = _context.Clock.Now.AddMinutes(14);

        // Act
        Func<Task> act = () => _service.QuoteAsync(_context.User.Id, new(_context.ShopId, _context.Location));

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _quoteStoreMock.Verify(r => r.SaveAsync(It.IsAny<CheckoutQuote>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID24 PASSED: CheckoutOrder_InsufficientPickupWindow_Rejects (Boundary).");
    }

    // TCID25_CheckoutOrder_ItemEligibilityChanged_RejectsBeforeRouting_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("hidden-deal")]
    [InlineData("hidden-variant")]
    [InlineData("inactive-variant")]
    [InlineData("hidden-product")]
    [InlineData("paused-shop")]
    [InlineData("negative-price")]
    [InlineData("zero-quantity")]
    [InlineData("over-stock")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID25_CheckoutOrder_ItemEligibilityChanged_RejectsBeforeRouting_Abnormal(string condition)
    {
        // Arrange
        var item = _context.Item; var v = item.FlashDealVariant;
        switch (condition)
        {
            case "hidden-deal": v.FlashDeal.Status = FlashDealStatus.Hidden; break;
            case "hidden-variant": v.Status = FlashDealVariantStatus.Hide; break;
            case "inactive-variant": v.Variant.IsActive = false; break;
            case "hidden-product": v.FlashDeal.Product.Status = ProductStatus.Hidden; break;
            case "paused-shop": v.FlashDeal.Shop.Status = ShopStatus.Paused; break;
            case "negative-price": v.DealPrice = -1; break;
            case "zero-quantity": item.Quantity = 0; break;
            case "over-stock": item.Quantity = 11; break;
        }

        // Act
        Func<Task> act = () => _service.QuoteAsync(_context.User.Id, new(_context.ShopId, _context.Location));

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _context.Travel.Verify(r => r.EstimateAsync(It.IsAny<CheckoutLocation>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID25 PASSED: CheckoutOrder_ItemEligibilityChanged_RejectsBeforeRouting (Abnormal).");
    }

    // TCID26_CheckoutOrder_ExactAvailableStock_CanPlace_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID26_CheckoutOrder_ExactAvailableStock_CanPlace_Boundary()
    {
        // Arrange
        _context.Item.FlashDealVariant.TotalQuantity = 4; _context.Item.FlashDealVariant.ReservedQuantity = 1;
        _context.Item.FlashDealVariant.SoldQuantity = 2;
        var request = await _context.Request();

        // Act
        var result = await _service.PlaceAsync(_context.User.Id, request);

        // Assert
        result.Items.Single().Quantity.Should().Be(1);
        _checkoutRepoMock.Verify(r => r.ReserveAsync(_context.Items, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID26 PASSED: CheckoutOrder_ExactAvailableStock_CanPlace (Boundary).");
    }

    // TCID27_CheckoutOrder_SaleStartsExactlyNow_AllowsQuote_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID27_CheckoutOrder_SaleStartsExactlyNow_AllowsQuote_Boundary()
    {
        // Arrange
        _context.Item.FlashDealVariant.FlashDeal.SaleStartTime = _context.Clock.Now;

        // Act
        var result = await _service.QuoteAsync(_context.User.Id, new(_context.ShopId, _context.Location));

        // Assert
        result.Shop.Subtotal.Should().Be(50000m);
        _output.WriteLine("TCID27 PASSED: CheckoutOrder_SaleStartsExactlyNow_AllowsQuote (Boundary).");
    }

    // TCID28_CheckoutOrder_InvalidOrUnavailableLevel_RequiresFullOnline_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("inactive-level")]
    [InlineData("missing-policy")]
    [InlineData("gap")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID28_CheckoutOrder_InvalidOrUnavailableLevel_RequiresFullOnline_Abnormal(string condition)
    {
        // Arrange
        if (condition == "inactive-level") _context.Level.IsActive = false;
        if (condition == "missing-policy") _context.Level.DepositPolicy = null;
        if (condition == "gap") _context.Level.DepositPolicy!.Ranges.Clear();

        // Act
        var result = await _service.ContextAsync(_context.User.Id, _context.ShopId);

        // Assert
        result.Policy.PayAtShopAllowed.Should().BeFalse();
        result.Policy.RestrictionCode.Should().Be("PolicyUnavailable");
        _output.WriteLine("TCID28 PASSED: CheckoutOrder_InvalidOrUnavailableLevel_RequiresFullOnline (Abnormal).");
    }

    // TCID29_CheckoutOrder_ExpirySchedulingFails_DoesNotStartPaymentOrPublish_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID29_CheckoutOrder_ExpirySchedulingFails_DoesNotStartPaymentOrPublish_Abnormal()
    {
        // Arrange
        var request = await _context.Request();
        _context.Scheduler.Setup(r => r.Schedule(It.IsAny<Guid>(), It.IsAny<DateTime>())).Throws(new InvalidOperationException("Scheduler unavailable."));

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _paymentMock.Verify(r => r.StartAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _context.Events.Verify(r => r.QueuePendingAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID29 PASSED: CheckoutOrder_ExpirySchedulingFails_DoesNotStartPaymentOrPublish (Abnormal).");
    }

    // TCID30_CheckoutOrder_OrderCodeAllocationFails_DoesNotReserve_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID30_CheckoutOrder_OrderCodeAllocationFails_DoesNotReserve_Abnormal()
    {
        // Arrange
        var request = await _context.Request();
        _context.Codes.Setup(r => r.NextAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ThrowsAsync(AppException.ServiceUnavailable("Code allocation unavailable."));

        // Act
        Func<Task> act = () => _service.PlaceAsync(_context.User.Id, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        _checkoutRepoMock.Verify(r => r.ReserveAsync(It.IsAny<IReadOnlyList<CartItem>>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID30 PASSED: CheckoutOrder_OrderCodeAllocationFails_DoesNotReserve (Abnormal).");
    }

    // TCID31_CheckoutOrder_QuoteInsideLastSecondOfTtl_CanPlace_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID31_CheckoutOrder_QuoteInsideLastSecondOfTtl_CanPlace_Boundary()
    {
        // Arrange
        var request = await _context.Request(); _context.Clock.Now = _context.Quote!.ExpiresAt.AddTicks(-1);

        // Act
        var result = await _service.PlaceAsync(_context.User.Id, request);

        // Assert
        result.Status.Should().Be("AwaitingPayment");
        result.HardDeadlineAt.Should().Be(_context.Quote!.Pickup!.HardDeadlineAt);
        _output.WriteLine("TCID31 PASSED: CheckoutOrder_QuoteInsideLastSecondOfTtl_CanPlace (Boundary).");
    }
}
