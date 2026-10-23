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
public class ViewItemsInCartTests
{
    private readonly ITestOutputHelper _output;
    private readonly CartTestContext _context;
    private readonly CartService _service;
    private readonly Mock<ICartRepository> _cartRepoMock;
    private readonly Mock<IRealtimePublisher> _realtimeMock;

    public ViewItemsInCartTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new CartTestContext();
        _service = _context.Service;
        _cartRepoMock = _context.Repo;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_ViewItemsInCart_GroupedItems_ReturnsServerPrices_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_ViewItemsInCart_GroupedItems_ReturnsServerPrices_Normal()
    {
        // Arrange
        var item = _context.Seed(quantity: 2);
        _cartRepoMock.Setup(r => r.GetPageAsync(_context.UserId, null, null, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<CartItem>)new[] { item }, false, (Guid?)null, (DateTime?)null));

        // Act
        var result = await _service.GetPageAsync(_context.UserId, null, 5);

        // Assert
        result.Page.Items.Should().ContainSingle();
        result.Page.Items[0].Subtotal.Should().Be(100000m);
        result.TotalQuantity.Should().Be(2);
        result.TotalFlashDeals.Should().Be(1);
        _cartRepoMock.Verify(r => r.GetPageAsync(_context.UserId, null, null, 5, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"TCID01 PASSED | StatusCode: {(int)HttpStatusCode.OK} {HttpStatusCode.OK} | ResponseData: TotalQuantity={result.TotalQuantity}, TotalFlashDeals={result.TotalFlashDeals}, ShopSubtotal={result.Page.Items[0].Subtotal} | JSON: {JsonSerializer.Serialize(result)}");
    }

    // TCID02_ViewItemsInCart_EmptyCart_ReturnsEmptyPage_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID02_ViewItemsInCart_EmptyCart_ReturnsEmptyPage_Boundary()
    {
        // Arrange
        _cartRepoMock.Setup(r => r.GetPageAsync(_context.UserId, null, null, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<CartItem>(), false, (Guid?)null, (DateTime?)null));

        // Act
        var result = await _service.GetPageAsync(_context.UserId, null, 5);

        // Assert
        result.Page.Items.Should().BeEmpty();
        result.Page.NextCursor.Should().BeNull();
        result.TotalQuantity.Should().Be(0);
        _output.WriteLine($"TCID02 PASSED | StatusCode: {(int)HttpStatusCode.OK} {HttpStatusCode.OK} | ResponseData: EmptyCart (ItemsCount={result.Page.Items.Count}, TotalQuantity={result.TotalQuantity}) | JSON: {JsonSerializer.Serialize(result)}");
    }

    // TCID03_ViewItemsInCart_PageSizeOutsideRange_ThrowsBadRequest_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(int.MaxValue)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID03_ViewItemsInCart_PageSizeOutsideRange_ThrowsBadRequest_Boundary(int pageSize)
    {
        // Arrange
        var userId = _context.UserId;

        // Act
        Func<Task> act = () => _service.GetPageAsync(userId, null, pageSize);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _cartRepoMock.Verify(r => r.GetPageAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine($"TCID03 PASSED | StatusCode: {(int)ex.Which.StatusCode} {ex.Which.StatusCode} | ErrorMessage: \"{ex.Which.Message}\" (Input pageSize={pageSize})");
    }

    // TCID04_ViewItemsInCart_MalformedCursor_ThrowsBadRequest_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("invalid")]
    [InlineData("%%%")]
    [InlineData("")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID04_ViewItemsInCart_MalformedCursor_ThrowsBadRequest_Abnormal(string cursor)
    {
        // Arrange
        var userId = _context.UserId;

        // Act
        Func<Task> act = () => _service.GetPageAsync(userId, cursor, 5);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine($"TCID04 PASSED | StatusCode: {(int)ex.Which.StatusCode} {ex.Which.StatusCode} | ErrorMessage: \"{ex.Which.Message}\" (Input cursor=\"{cursor}\")");
    }

    // TCID05_ViewItemsInCart_Cursor_UsesLastShopAndAddedTime_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID05_ViewItemsInCart_Cursor_UsesLastShopAndAddedTime_Normal()
    {
        // Arrange
        var item = _context.Seed();
        var shopId = item.FlashDealVariant.FlashDeal.ShopId;
        _cartRepoMock.Setup(r => r.GetPageAsync(_context.UserId, null, null, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<CartItem>)new[] { item }, true, (Guid?)shopId, (DateTime?)item.AddedAt));
        _cartRepoMock.Setup(r => r.GetPageAsync(_context.UserId, shopId, item.AddedAt, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<CartItem>(), false, (Guid?)null, (DateTime?)null));

        // Act
        var first = await _service.GetPageAsync(_context.UserId, null, 1);
        var second = await _service.GetPageAsync(_context.UserId, first.Page.NextCursor, 1);

        // Assert
        first.Page.HasMore.Should().BeTrue();
        second.Page.HasMore.Should().BeFalse();
        _cartRepoMock.Verify(r => r.GetPageAsync(_context.UserId, shopId, item.AddedAt, 1, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"TCID05 PASSED | StatusCode: {(int)HttpStatusCode.OK} {HttpStatusCode.OK} | ResponseData: Page1.HasMore={first.Page.HasMore}, Page2.HasMore={second.Page.HasMore}, NextCursor={first.Page.NextCursor}");
    }

    // TCID06_ViewItemsInCart_ExpiredDeal_RemainsVisibleAsUnavailable_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_ViewItemsInCart_ExpiredDeal_RemainsVisibleAsUnavailable_Abnormal()
    {
        // Arrange
        var item = _context.Seed(); item.FlashDealVariant.FlashDeal.OrderEndTime = DateTime.UtcNow.AddMinutes(-1);
        _cartRepoMock.Setup(r => r.GetPageAsync(_context.UserId, null, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<CartItem>)new[] { item }, false, (Guid?)null, (DateTime?)null));

        // Act
        var result = await _service.GetPageAsync(_context.UserId, null, 20);

        // Assert
        result.Page.Items[0].Items[0].IsAvailable.Should().BeFalse();
        result.Page.Items[0].CanCheckout.Should().BeFalse();
        _cartRepoMock.Verify(r => r.Remove(It.IsAny<CartItem>()), Times.Never);
        _output.WriteLine($"TCID06 PASSED | StatusCode: {(int)HttpStatusCode.OK} {HttpStatusCode.OK} | ResponseData: IsAvailable={result.Page.Items[0].Items[0].IsAvailable}, CanCheckout={result.Page.Items[0].CanCheckout} (Deal expired but remains visible in cart)");
    }

    // TCID07_ViewItemsInCart_Summary_CountsDealsSeparatelyFromQuantity_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID07_ViewItemsInCart_Summary_CountsDealsSeparatelyFromQuantity_Normal()
    {
        // Arrange
        _context.Seed(quantity: 4);

        // Act
        var result = await _service.GetSummaryAsync(_context.UserId);

        // Assert
        result.TotalQuantity.Should().Be(4);
        result.TotalFlashDeals.Should().Be(1);
        _cartRepoMock.Verify(r => r.GetSummaryAsync(_context.UserId, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"TCID07 PASSED | StatusCode: {(int)HttpStatusCode.OK} {HttpStatusCode.OK} | ResponseData: TotalQuantity={result.TotalQuantity}, TotalFlashDeals={result.TotalFlashDeals} | JSON: {JsonSerializer.Serialize(result)}");
    }

    // TCID08_ViewItemsInCart_NewerShop_IsDisplayedBeforeOlderShop_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID08_ViewItemsInCart_NewerShop_IsDisplayedBeforeOlderShop_Normal()
    {
        // Arrange
        var older = _context.Seed(); var newer = _context.Seed(); older.AddedAt = newer.AddedAt.AddMinutes(-1);
        _cartRepoMock.Setup(r => r.GetPageAsync(_context.UserId, null, null, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<CartItem>)new[] { older, newer }, false, (Guid?)null, (DateTime?)null));

        // Act
        var result = await _service.GetPageAsync(_context.UserId, null, 5);

        // Assert
        result.Page.Items.Select(x => x.ShopId).Should().Equal(newer.FlashDealVariant.FlashDeal.ShopId, older.FlashDealVariant.FlashDeal.ShopId);
        _output.WriteLine($"TCID08 PASSED | StatusCode: {(int)HttpStatusCode.OK} {HttpStatusCode.OK} | ResponseData: FirstShop={result.Page.Items[0].ShopId} (newer), SecondShop={result.Page.Items[1].ShopId} (older)");
    }
}
