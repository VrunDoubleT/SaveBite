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
public class UpdateItemQuantityTests
{
    private readonly ITestOutputHelper _output;
    private readonly CartTestContext _context;
    private readonly CartService _service;
    private readonly Mock<ICartRepository> _cartRepoMock;
    private readonly Mock<IRealtimePublisher> _realtimeMock;

    public UpdateItemQuantityTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new CartTestContext();
        _service = _context.Service;
        _cartRepoMock = _context.Repo;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_UpdateItemQuantity_ValidQuantity_ReturnsOnlyChangedShop_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_UpdateItemQuantity_ValidQuantity_ReturnsOnlyChangedShop_Normal()
    {
        // Arrange
        var item = _context.Seed(); var other = _context.Seed(quantity: 3);

        // Act
        var result = await _service.UpdateQuantityAsync(_context.UserId, item.Id, new(2));

        // Assert
        item.Quantity.Should().Be(2);
        other.Quantity.Should().Be(3);
        result.ShopId.Should().Be(item.FlashDealVariant.FlashDeal.ShopId);
        result.Shop!.Items.Should().ContainSingle();
        result.Summary.TotalQuantity.Should().Be(5);
        _cartRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _realtimeMock.Verify(r => r.SendToUserAsync(_context.UserId, RealtimeEvents.CartChanged, It.IsAny<object>()), Times.Once);
        _output.WriteLine($"TCID01 PASSED | StatusCode: {(int)HttpStatusCode.OK} {HttpStatusCode.OK} | ResponseData: ShopId={result.ShopId}, UpdatedItemQuantity={item.Quantity}, CartTotalQuantity={result.Summary.TotalQuantity} | JSON: {JsonSerializer.Serialize(result)}");
    }

    // TCID02_UpdateItemQuantity_NonPositive_ThrowsBadRequest_Boundary: verify the expected business result and dependency interactions.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [Trait("CaseType", "Boundary")]
    public async Task TCID02_UpdateItemQuantity_NonPositive_ThrowsBadRequest_Boundary(int quantity)
    {
        // Arrange
        var item = _context.Seed();

        // Act
        Func<Task> act = () => _service.UpdateQuantityAsync(_context.UserId, item.Id, new(quantity));

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _cartRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine($"TCID02 PASSED | StatusCode: {(int)ex.Which.StatusCode} {ex.Which.StatusCode} | ErrorMessage: \"{ex.Which.Message}\" (Input quantity={quantity})");
    }

    // TCID03_UpdateItemQuantity_ForeignItem_ThrowsNotFound_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_UpdateItemQuantity_ForeignItem_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var item = _context.Seed(); item.UserId = Guid.NewGuid();

        // Act
        Func<Task> act = () => _service.UpdateQuantityAsync(_context.UserId, item.Id, new(2));

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        item.Quantity.Should().Be(1);
        _output.WriteLine($"TCID03 PASSED | StatusCode: {(int)ex.Which.StatusCode} {ex.Which.StatusCode} | ErrorMessage: \"{ex.Which.Message}\" (Item belongs to other customer)");
    }

    // TCID04_UpdateItemQuantity_ExactlyAvailableStock_Succeeds_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID04_UpdateItemQuantity_ExactlyAvailableStock_Succeeds_Boundary()
    {
        // Arrange
        var item = _context.Seed(); item.FlashDealVariant.ReservedQuantity = 2; item.FlashDealVariant.SoldQuantity = 3;

        // Act
        var result = await _service.UpdateQuantityAsync(_context.UserId, item.Id, new(5));

        // Assert
        result.Shop!.Items[0].Quantity.Should().Be(5);
        item.FlashDealVariant.ReservedQuantity.Should().Be(2);
        _output.WriteLine($"TCID04 PASSED | StatusCode: {(int)HttpStatusCode.OK} {HttpStatusCode.OK} | ResponseData: UpdatedQuantity={result.Shop!.Items[0].Quantity} (Exactly available stock 10-2-3=5)");
    }

    // TCID05_UpdateItemQuantity_OverAvailableStock_RejectsWithoutSaving_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID05_UpdateItemQuantity_OverAvailableStock_RejectsWithoutSaving_Boundary()
    {
        // Arrange
        var item = _context.Seed(); item.FlashDealVariant.ReservedQuantity = 2; item.FlashDealVariant.SoldQuantity = 3;

        // Act
        Func<Task> act = () => _service.UpdateQuantityAsync(_context.UserId, item.Id, new(6));

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        item.Quantity.Should().Be(1);
        _cartRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine($"TCID05 PASSED | StatusCode: {(int)ex.Which.StatusCode} {ex.Which.StatusCode} | ErrorMessage: \"{ex.Which.Message}\" (Requested quantity 6 > available 5)");
    }

    // TCID06_UpdateItemQuantity_OwnShop_ThrowsForbidden_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID06_UpdateItemQuantity_OwnShop_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var item = _context.Seed();
        _cartRepoMock.Setup(r => r.IsShopOperatorAsync(_context.UserId, item.FlashDealVariant.FlashDeal.ShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        Func<Task> act = () => _service.UpdateQuantityAsync(_context.UserId, item.Id, new(2));

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _cartRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine($"TCID06 PASSED | StatusCode: {(int)ex.Which.StatusCode} {ex.Which.StatusCode} | ErrorMessage: \"{ex.Which.Message}\" (User is shop operator)");
    }

    // TCID07_UpdateItemQuantity_ExpiredDeal_Rejects_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID07_UpdateItemQuantity_ExpiredDeal_Rejects_Abnormal()
    {
        // Arrange
        var item = _context.Seed(); item.FlashDealVariant.FlashDeal.OrderEndTime = DateTime.UtcNow.AddMinutes(-1);

        // Act
        Func<Task> act = () => _service.UpdateQuantityAsync(_context.UserId, item.Id, new(2));

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        _realtimeMock.Verify(r => r.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _output.WriteLine($"TCID07 PASSED | StatusCode: {(int)ex.Which.StatusCode} {ex.Which.StatusCode} | ErrorMessage: \"{ex.Which.Message}\" (OrderEndTime passed)");
    }

    // TCID08_UpdateItemQuantity_VariantMissing_ThrowsNotFound_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID08_UpdateItemQuantity_VariantMissing_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var item = _context.Seed(); _context.Variants.Clear();

        // Act
        Func<Task> act = () => _service.UpdateQuantityAsync(_context.UserId, item.Id, new(2));

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _cartRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine($"TCID08 PASSED | StatusCode: {(int)ex.Which.StatusCode} {ex.Which.StatusCode} | ErrorMessage: \"{ex.Which.Message}\" (Variant removed from catalog)");
    }
}
