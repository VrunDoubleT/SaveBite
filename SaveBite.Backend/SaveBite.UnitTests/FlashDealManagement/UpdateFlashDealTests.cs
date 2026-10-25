using System.Net;
using System.Text.Json;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FlashDealManagement;

/// <summary>
/// Unit Tests for Feature: Update Flash Deal
/// Service: FlashDealService.UpdateDealAsync
/// Function Code: UT-79
/// Sheet Name: UT_UpdateFlashDeal
/// </summary>
public class UpdateFlashDealTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFlashDealRedisService> _redisMock = new();
    private readonly Mock<IFlashDealRepository> _dealRepoMock = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly Mock<ILogger<FlashDealService>> _loggerMock = new();
    private readonly FlashDealService _service;

    // Fixed Test Identifiers
    private readonly Guid _ownerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _existingDealId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly Guid _nonExistentDealId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private readonly Guid _productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _variantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public UpdateFlashDealTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FlashDealService(
            _redisMock.Object,
            _dealRepoMock.Object,
            _userAddressRepoMock.Object,
            _shopRepoMock.Object,
            _staffRepoMock.Object,
            _backgroundJobClientMock.Object,
            _publisherMock.Object,
            _loggerMock.Object
        );
    }

    private Shop CreateSampleShop()
    {
        return new Shop
        {
            Id = _shopId,
            OwnerUserId = _ownerUserId,
            Name = "SaveBite Bakery",
            AddressLine = "123 Le Loi Street",
            Latitude = 10.7769,
            Longitude = 106.7009,
            Status = ShopStatus.Active
        };
    }

    private Product CreateSampleProduct()
    {
        return new Product
        {
            Id = _productId,
            ShopId = _shopId,
            Name = "Banh Mi Pate Dac Biet",
            Status = ProductStatus.Active,
            Variants = new List<ProductVariant>
            {
                new()
                {
                    Id = _variantId,
                    ProductId = _productId,
                    Sku = "BM-01",
                    OriginalPrice = 30000m,
                    DealPrice = 15000m,
                    IsActive = true
                }
            }
        };
    }

    private FlashDeal CreateSampleDeal(FlashDealStatus status = FlashDealStatus.Pending, int soldQuantity = 5, int reservedQuantity = 5)
    {
        var now = DateTime.UtcNow;
        var deal = new FlashDeal
        {
            Id = _existingDealId,
            ShopId = _shopId,
            ProductId = _productId,
            SaleStartTime = now.AddMinutes(30),
            OrderEndTime = now.AddMinutes(90),
            ShopClosingTime = now.AddMinutes(120),
            Status = status,
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now.AddHours(-1),
            Product = CreateSampleProduct(),
            Variants = new List<FlashDealVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FlashDealId = _existingDealId,
                    VariantId = _variantId,
                    OriginalPrice = 30000m,
                    DealPrice = 15000m,
                    DiscountPercent = 50m,
                    TotalQuantity = 20,
                    ReservedQuantity = reservedQuantity,
                    SoldQuantity = soldQuantity,
                    Status = FlashDealVariantStatus.Active
                }
            }
        };
        return deal;
    }

    // =========================================================================
    // UTCID01: Store Owner updates deal schedule and quantity successfully (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_UpdateDeal_ValidPendingDeal_ReturnsUpdatedDeal_Normal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new UpdateFlashDealRequest
        {
            SaleStartTime = now.AddMinutes(30),
            OrderEndTime = now.AddMinutes(90),
            ShopClosingTime = now.AddMinutes(120),
            Variants = new List<UpdateFlashDealVariantRequest>
            {
                new() { VariantId = _variantId, DealPrice = 14000m, TotalQuantity = 25, Status = FlashDealVariantStatus.Active }
            }
        };

        var shop = CreateSampleShop();
        var product = CreateSampleProduct();
        var deal = CreateSampleDeal(FlashDealStatus.Pending);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _dealRepoMock
            .Setup(r => r.GetProductForDealCreationAsync(_shopId, _productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _dealRepoMock
            .Setup(r => r.HasOverlappingDealAsync(_shopId, _productId, request.SaleStartTime, request.OrderEndTime, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _dealRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<FlashDeal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateDealAsync(_ownerUserId, _existingDealId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingDealId);
        result.Variants[0].DealPrice.Should().Be(14000m);
        result.Variants[0].TotalQuantity.Should().Be(25);
    }

    // =========================================================================
    // UTCID02: Deal does not exist in shop -> Throws NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID02_UpdateDeal_DealNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new UpdateFlashDealRequest
        {
            SaleStartTime = now.AddMinutes(30),
            OrderEndTime = now.AddMinutes(90),
            ShopClosingTime = now.AddMinutes(120),
            Variants = new List<UpdateFlashDealVariantRequest>()
        };

        var shop = CreateSampleShop();

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _nonExistentDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlashDeal?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateDealAsync(_ownerUserId, _nonExistentDealId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID02", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Flash deal not found.");
    }

    // =========================================================================
    // UTCID03: Cannot update closed or cancelled flash deal -> Throws BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_UpdateDeal_ClosedDeal_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new UpdateFlashDealRequest
        {
            SaleStartTime = now.AddMinutes(30),
            OrderEndTime = now.AddMinutes(90),
            ShopClosingTime = now.AddMinutes(120),
            Variants = new List<UpdateFlashDealVariantRequest>()
        };

        var shop = CreateSampleShop();
        var product = CreateSampleProduct();
        var deal = CreateSampleDeal(FlashDealStatus.Closed);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _dealRepoMock
            .Setup(r => r.GetProductForDealCreationAsync(_shopId, _productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        Func<Task> act = async () => await _service.UpdateDealAsync(_ownerUserId, _existingDealId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID03", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be("Cannot update a closed or cancelled flash deal.");
    }

    // =========================================================================
    // UTCID04: Overlapping deal exists in updated schedule -> Throws BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_UpdateDeal_OverlappingDeal_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new UpdateFlashDealRequest
        {
            SaleStartTime = now.AddMinutes(30),
            OrderEndTime = now.AddMinutes(90),
            ShopClosingTime = now.AddMinutes(120),
            Variants = new List<UpdateFlashDealVariantRequest>()
        };

        var shop = CreateSampleShop();
        var product = CreateSampleProduct();
        var deal = CreateSampleDeal(FlashDealStatus.Pending);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _dealRepoMock
            .Setup(r => r.GetProductForDealCreationAsync(_shopId, _productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _dealRepoMock
            .Setup(r => r.HasOverlappingDealAsync(_shopId, _productId, request.SaleStartTime, request.OrderEndTime, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _service.UpdateDealAsync(_ownerUserId, _existingDealId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID04", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be("Another flash deal already overlaps with the updated schedule.");
    }

    // =========================================================================
    // UTCID05: TotalQuantity less than already sold/reserved quantity -> Throws BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_UpdateDeal_TotalQuantityLessThanSoldPlusReserved_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        // SoldQuantity = 5, ReservedQuantity = 5 -> Total cannot be less than 10
        var deal = CreateSampleDeal(FlashDealStatus.Pending, soldQuantity: 5, reservedQuantity: 5);
        var request = new UpdateFlashDealRequest
        {
            SaleStartTime = now.AddMinutes(30),
            OrderEndTime = now.AddMinutes(90),
            ShopClosingTime = now.AddMinutes(120),
            Variants = new List<UpdateFlashDealVariantRequest>
            {
                new() { VariantId = _variantId, DealPrice = 14000m, TotalQuantity = 8, Status = FlashDealVariantStatus.Active } // 8 < 10!
            }
        };

        var shop = CreateSampleShop();
        var product = CreateSampleProduct();

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        _dealRepoMock
            .Setup(r => r.GetProductForDealCreationAsync(_shopId, _productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _dealRepoMock
            .Setup(r => r.HasOverlappingDealAsync(_shopId, _productId, request.SaleStartTime, request.OrderEndTime, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.UpdateDealAsync(_ownerUserId, _existingDealId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID05", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Contain("New total quantity cannot be less than already sold/reserved quantity");
    }

    private void PrintSuccess<T>(string tcid, T result)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }

    private void PrintError(string tcid, int statusCode, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {statusCode}");
        _output.WriteLine($"ERROR: {message}");
        _output.WriteLine("========================================");
    }
}
