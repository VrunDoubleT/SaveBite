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
/// Unit Tests for Feature: Create Flash Deal
/// Service: FlashDealService.CreateDealAsync
/// Function Code: UT-78
/// Sheet Name: UT_CreateFlashDeal
/// </summary>
public class CreateFlashDealTests
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
    private readonly Guid _productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _nonExistentProductId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private readonly Guid _variantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public CreateFlashDealTests(ITestOutputHelper output)
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

    private Product CreateSampleProduct(Guid productId, ProductStatus status = ProductStatus.Active)
    {
        return new Product
        {
            Id = productId,
            ShopId = _shopId,
            Name = "Banh Mi Pate Dac Biet",
            Description = "Banh mi gion ngon",
            Status = status,
            Variants = new List<ProductVariant>
            {
                new()
                {
                    Id = _variantId,
                    ProductId = productId,
                    Sku = "BM-01",
                    OriginalPrice = 30000m,
                    DealPrice = 15000m,
                    IsActive = true
                }
            }
        };
    }

    // =========================================================================
    // UTCID01: Create immediate flash deal successfully (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_CreateDeal_ImmediateDealValid_ReturnsCreatedDeal_Normal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new CreateFlashDealRequest
        {
            ProductId = _productId,
            IsImmediate = true,
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(3),
            Variants = new List<CreateFlashDealVariantRequest>
            {
                new() { VariantId = _variantId, DealPrice = 15000m, TotalQuantity = 20 }
            }
        };

        var shop = CreateSampleShop();
        var product = CreateSampleProduct(_productId);
        var createdDeal = new FlashDeal
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            ProductId = _productId,
            SaleStartTime = now,
            OrderEndTime = request.OrderEndTime,
            ShopClosingTime = request.ShopClosingTime,
            Status = FlashDealStatus.OnSale,
            Product = product,
            Variants = new List<FlashDealVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    VariantId = _variantId,
                    OriginalPrice = 30000m,
                    DealPrice = 15000m,
                    DiscountPercent = 50m,
                    TotalQuantity = 20,
                    Status = FlashDealVariantStatus.Active
                }
            }
        };

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.HasOverlappingDealAsync(_shopId, _productId, It.IsAny<DateTime>(), request.OrderEndTime, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _dealRepoMock
            .Setup(r => r.GetProductForDealCreationAsync(_shopId, _productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _dealRepoMock
            .Setup(r => r.AddAsync(It.IsAny<FlashDeal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDeal);

        // Act
        var result = await _service.CreateDealAsync(_ownerUserId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.ProductId.Should().Be(_productId);
        result.Status.Should().Be(FlashDealStatus.OnSale);
        result.Variants.Should().HaveCount(1);
        result.Variants[0].DealPrice.Should().Be(15000m);
    }

    // =========================================================================
    // UTCID02: SaleStartTime is after OrderEndTime -> Throws BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID02_CreateDeal_SaleStartTimeAfterOrderEndTime_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new CreateFlashDealRequest
        {
            ProductId = _productId,
            IsImmediate = false,
            SaleStartTime = now.AddHours(3),
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(4),
            Variants = new List<CreateFlashDealVariantRequest>
            {
                new() { VariantId = _variantId, DealPrice = 15000m, TotalQuantity = 20 }
            }
        };

        var shop = CreateSampleShop();

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        // Act
        Func<Task> act = async () => await _service.CreateDealAsync(_ownerUserId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID02", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be("Sale start time must be before order end time.");
    }

    // =========================================================================
    // UTCID03: Active/pending deal already overlaps in selected time range -> Throws BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_CreateDeal_OverlappingDealExists_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new CreateFlashDealRequest
        {
            ProductId = _productId,
            IsImmediate = true,
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(3),
            Variants = new List<CreateFlashDealVariantRequest>
            {
                new() { VariantId = _variantId, DealPrice = 15000m, TotalQuantity = 20 }
            }
        };

        var shop = CreateSampleShop();

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.HasOverlappingDealAsync(_shopId, _productId, It.IsAny<DateTime>(), request.OrderEndTime, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _service.CreateDealAsync(_ownerUserId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID03", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be("An active or pending flash deal already exists for this product in the selected time range.");
    }

    // =========================================================================
    // UTCID04: Product not found or does not belong to shop -> Throws NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_CreateDeal_ProductNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new CreateFlashDealRequest
        {
            ProductId = _nonExistentProductId,
            IsImmediate = true,
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(3),
            Variants = new List<CreateFlashDealVariantRequest>
            {
                new() { VariantId = _variantId, DealPrice = 15000m, TotalQuantity = 20 }
            }
        };

        var shop = CreateSampleShop();

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.HasOverlappingDealAsync(_shopId, _nonExistentProductId, It.IsAny<DateTime>(), request.OrderEndTime, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _dealRepoMock
            .Setup(r => r.GetProductForDealCreationAsync(_shopId, _nonExistentProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.CreateDealAsync(_ownerUserId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID04", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Product not found or does not belong to your shop.");
    }

    // =========================================================================
    // UTCID05: Deal price is greater than or equal to original price -> Throws BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_CreateDeal_DealPriceNotLessThanOriginal_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var request = new CreateFlashDealRequest
        {
            ProductId = _productId,
            IsImmediate = true,
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(3),
            Variants = new List<CreateFlashDealVariantRequest>
            {
                new() { VariantId = _variantId, DealPrice = 35000m, TotalQuantity = 20 }
            }
        };

        var shop = CreateSampleShop();
        var product = CreateSampleProduct(_productId);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.HasOverlappingDealAsync(_shopId, _productId, It.IsAny<DateTime>(), request.OrderEndTime, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _dealRepoMock
            .Setup(r => r.GetProductForDealCreationAsync(_shopId, _productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        Func<Task> act = async () => await _service.CreateDealAsync(_ownerUserId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID05", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Contain("must be less than original price");
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
