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
public class RemoveItemFromCartTests
{
    private readonly ITestOutputHelper _output;
    private readonly CartTestContext _context;
    private readonly CartService _service;
    private readonly Mock<ICartRepository> _cartRepoMock;
    private readonly Mock<IRealtimePublisher> _realtimeMock;

    public RemoveItemFromCartTests(ITestOutputHelper output)
    {
        _output = output;
        _context = new CartTestContext();
        _service = _context.Service;
        _cartRepoMock = _context.Repo;
        _realtimeMock = _context.Realtime;
    }

    // TCID01_RemoveItemFromCart_OwnItem_KeepsOtherShops_Normal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Normal")]
    public async Task TCID01_RemoveItemFromCart_OwnItem_KeepsOtherShops_Normal()
    {
        // Arrange
        var item = _context.Seed(); var other = _context.Seed(quantity: 2);

        // Act
        var result = await _service.RemoveItemAsync(_context.UserId, item.Id);

        // Assert
        _context.Items.Should().ContainSingle().Which.Id.Should().Be(other.Id);
        result.Shop.Should().BeNull();
        result.Summary.TotalQuantity.Should().Be(2);
        _cartRepoMock.Verify(r => r.Remove(item), Times.Once);
        _realtimeMock.Verify(r => r.SendToUserAsync(_context.UserId, RealtimeEvents.CartChanged, It.IsAny<object>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: RemoveItemFromCart_OwnItem_KeepsOtherShops (Normal).");
    }

    // TCID02_RemoveItemFromCart_LastItem_ReturnsEmptyShopAndZeroSummary_Boundary: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Boundary")]
    public async Task TCID02_RemoveItemFromCart_LastItem_ReturnsEmptyShopAndZeroSummary_Boundary()
    {
        // Arrange
        var item = _context.Seed();

        // Act
        var result = await _service.RemoveItemAsync(_context.UserId, item.Id);

        // Assert
        result.Shop.Should().BeNull();
        result.Summary.TotalQuantity.Should().Be(0);
        result.Summary.TotalFlashDeals.Should().Be(0);
        _output.WriteLine("TCID02 PASSED: RemoveItemFromCart_LastItem_ReturnsEmptyShopAndZeroSummary (Boundary).");
    }

    // TCID03_RemoveItemFromCart_ForeignItem_ThrowsNotFound_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID03_RemoveItemFromCart_ForeignItem_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var item = _context.Seed(); item.UserId = Guid.NewGuid();

        // Act
        Func<Task> act = () => _service.RemoveItemAsync(_context.UserId, item.Id);

        // Assert
        (await act.Should().ThrowAsync<AppException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _cartRepoMock.Verify(r => r.Remove(It.IsAny<CartItem>()), Times.Never);
        _output.WriteLine("TCID03 PASSED: RemoveItemFromCart_ForeignItem_ThrowsNotFound (Abnormal).");
    }

    // TCID04_RemoveItemFromCart_ExpiredOwnShopItem_StillRemovable_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID04_RemoveItemFromCart_ExpiredOwnShopItem_StillRemovable_Abnormal()
    {
        // Arrange
        var item = _context.Seed(); item.FlashDealVariant.FlashDeal.OrderEndTime = DateTime.UtcNow.AddHours(-1);
        _cartRepoMock.Setup(r => r.IsShopOperatorAsync(_context.UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        await _service.RemoveItemAsync(_context.UserId, item.Id);

        // Assert
        _context.Items.Should().BeEmpty();
        _cartRepoMock.Verify(r => r.IsShopOperatorAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _output.WriteLine("TCID04 PASSED: RemoveItemFromCart_ExpiredOwnShopItem_StillRemovable (Abnormal).");
    }

    // TCID05_RemoveItemFromCart_MissingItem_DoesNotPublish_Abnormal: verify the expected business result and dependency interactions.
    [Fact]
    [Trait("CaseType", "Abnormal")]
    public async Task TCID05_RemoveItemFromCart_MissingItem_DoesNotPublish_Abnormal()
    {
        // Arrange
        var itemId = Guid.NewGuid();

        // Act
        Func<Task> act = () => _service.RemoveItemAsync(_context.UserId, itemId);

        // Assert
        await act.Should().ThrowAsync<AppException>();
        _cartRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _realtimeMock.Verify(r => r.SendToUserAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _output.WriteLine("TCID05 PASSED: RemoveItemFromCart_MissingItem_DoesNotPublish (Abnormal).");
    }
}
