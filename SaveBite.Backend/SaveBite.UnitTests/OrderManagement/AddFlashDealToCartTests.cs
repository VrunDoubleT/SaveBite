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

namespace SaveBite.Api.Tests.FlashDealManagement;

[Trait("Category", "Unit")]
public class AddFlashDealToCartTests
{
    private readonly ITestOutputHelper _output;
    private readonly CartTestContext _context;
    private readonly CartService _service;
    private readonly Mock<ICartRepository> _cartRepoMock;
    private readonly Mock<IRealtimePublisher> _realtimeMock;

    public AddFlashDealToCartTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new CartTestContext();
        _service = _context.Service;
        _cartRepoMock = _context.Repo;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_AddFlashDealToCart_NewVariant_AddsWithoutReserving_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_AddFlashDealToCart_NewVariant_AddsWithoutReserving_Normal()
    {
        // Arrange
        var item = _context.Seed(inCart: false);

        // Act
        var result = await _service.AddItemAsync(_context.UserId, new(item.FlashDealVariantId, 2));

        // Assert
        result.TotalQuantity.Should().Be(2);
        _context.Items.Should().ContainSingle();
        item.FlashDealVariant.ReservedQuantity.Should().Be(0);
        _cartRepoMock.Verify(r => r.Add(It.Is<CartItem>(x => x.UserId == _context.UserId && x.Quantity == 2)), Times.Once);
        _realtimeMock.Verify(r => r.SendToUserAsync(_context.UserId, RealtimeEvents.CartChanged, It.IsAny<object>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: AddFlashDealToCart_NewVariant_AddsWithoutReserving (Normal).");
    }

    // TCID02_AddFlashDealToCart_ExistingVariant_MergesQuantity_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID02_AddFlashDealToCart_ExistingVariant_MergesQuantity_Normal()
    {
        // Arrange
        var item = _context.Seed(quantity: 2);

        // Act
        var result = await _service.AddItemAsync(_context.UserId, new(item.FlashDealVariantId, 3));

        // Assert
        item.Quantity.Should().Be(5);
        result.TotalQuantity.Should().Be(5);
        _cartRepoMock.Verify(r => r.Add(It.IsAny<CartItem>()), Times.Never);
        _output.WriteLine("TCID02 PASSED: AddFlashDealToCart_ExistingVariant_MergesQuantity (Normal).");
    }

    // TCID03_AddFlashDealToCart_InvalidQuantity_ThrowsBadRequest_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID03_AddFlashDealToCart_InvalidQuantity_ThrowsBadRequest_Boundary(int quantity)
    {
        // Arrange
        var variantId = Guid.NewGuid();

        // Act
        Func<Task> act = () => _service.AddItemAsync(_context.UserId, new(variantId, quantity));

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _cartRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID03 PASSED: AddFlashDealToCart_InvalidQuantity_ThrowsBadRequest (Boundary).");
    }

    // TCID04_AddFlashDealToCart_EmptyVariantId_ThrowsBadRequest_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID04_AddFlashDealToCart_EmptyVariantId_ThrowsBadRequest_Boundary()
    {
        // Arrange
        var request = new AddCartItemRequest(Guid.Empty, 1);

        // Act
        Func<Task> act = () => _service.AddItemAsync(_context.UserId, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _output.WriteLine("TCID04 PASSED: AddFlashDealToCart_EmptyVariantId_ThrowsBadRequest (Boundary).");
    }

    // TCID05_AddFlashDealToCart_MissingVariant_ThrowsNotFound_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID05_AddFlashDealToCart_MissingVariant_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new AddCartItemRequest(Guid.NewGuid(), 1);

        // Act
        Func<Task> act = () => _service.AddItemAsync(_context.UserId, request);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _output.WriteLine("TCID05 PASSED: AddFlashDealToCart_MissingVariant_ThrowsNotFound (Abnormal).");
    }

    // TCID06_AddFlashDealToCart_ShopOperator_ThrowsForbidden_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_AddFlashDealToCart_ShopOperator_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var item = _context.Seed(inCart: false);
        _cartRepoMock.Setup(r => r.IsShopOperatorAsync(_context.UserId, item.FlashDealVariant.FlashDeal.ShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        Func<Task> act = () => _service.AddItemAsync(_context.UserId, new(item.FlashDealVariantId, 1));

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _cartRepoMock.Verify(r => r.Add(It.IsAny<CartItem>()), Times.Never);
        _output.WriteLine("TCID06 PASSED: AddFlashDealToCart_ShopOperator_ThrowsForbidden (Abnormal).");
    }

    // TCID07_AddFlashDealToCart_UnavailableVariant_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData("hidden")]
    [InlineData("future")]
    [InlineData("expired")]
    [InlineData("inactive-variant")]
    [InlineData("inactive-product")]
    [InlineData("inactive-shop")]
    [InlineData("sold-out")]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID07_AddFlashDealToCart_UnavailableVariant_Rejects_Abnormal(string condition)
    {
        // Arrange
        var item = _context.Seed(inCart: false); var variant = item.FlashDealVariant;
        switch (condition)
        {
            case "hidden": variant.FlashDeal.Status = FlashDealStatus.Hidden; break;
            case "future": variant.FlashDeal.SaleStartTime = DateTime.UtcNow.AddHours(1); break;
            case "expired": variant.FlashDeal.OrderEndTime = DateTime.UtcNow.AddSeconds(-1); break;
            case "inactive-variant": variant.Variant.IsActive = false; break;
            case "inactive-product": variant.FlashDeal.Product.Status = ProductStatus.Hidden; break;
            case "inactive-shop": variant.FlashDeal.Shop.Status = ShopStatus.Paused; break;
            case "sold-out": variant.SoldQuantity = variant.TotalQuantity; break;
        }

        // Act
        Func<Task> act = () => _service.AddItemAsync(_context.UserId, new(variant.Id, 1));

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _cartRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID07 PASSED: AddFlashDealToCart_UnavailableVariant_Rejects (Abnormal).");
    }

    // TCID08_AddFlashDealToCart_ExactAvailableQuantity_Succeeds_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID08_AddFlashDealToCart_ExactAvailableQuantity_Succeeds_Boundary()
    {
        // Arrange
        var item = _context.Seed(inCart: false); item.FlashDealVariant.ReservedQuantity = 2; item.FlashDealVariant.SoldQuantity = 3;

        // Act
        var result = await _service.AddItemAsync(_context.UserId, new(item.FlashDealVariantId, 5));

        // Assert
        result.TotalQuantity.Should().Be(5);
        item.FlashDealVariant.SoldQuantity.Should().Be(3);
        _output.WriteLine("TCID08 PASSED: AddFlashDealToCart_ExactAvailableQuantity_Succeeds (Boundary).");
    }

    // TCID09_AddFlashDealToCart_OverflowCombinedQuantity_RejectsWithoutMutation_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID09_AddFlashDealToCart_OverflowCombinedQuantity_RejectsWithoutMutation_Boundary()
    {
        // Arrange
        var item = _context.Seed();

        // Act
        Func<Task> act = () => _service.AddItemAsync(_context.UserId, new(item.FlashDealVariantId, int.MaxValue));

        // Assert
        await act.Should().ThrowAsync<AppException>();
        item.Quantity.Should().Be(1);
        _cartRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID09 PASSED: AddFlashDealToCart_OverflowCombinedQuantity_RejectsWithoutMutation (Boundary).");
    }

    // TCID10_AddFlashDealToCart_SaveFailure_DoesNotSendRealtime_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID10_AddFlashDealToCart_SaveFailure_DoesNotSendRealtime_Abnormal()
    {
        // Arrange
        var item = _context.Seed(inCart: false);
        _cartRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Persistence failed."));

        // Act
        Func<Task> act = () => _service.AddItemAsync(_context.UserId, new(item.FlashDealVariantId, 1));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _realtimeMock.Verify(r => r.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _output.WriteLine("TCID10 PASSED: AddFlashDealToCart_SaveFailure_DoesNotSendRealtime (Abnormal).");
    }
}
