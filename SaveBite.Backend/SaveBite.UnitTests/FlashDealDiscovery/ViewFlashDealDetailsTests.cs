using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FlashDealDiscovery;

/// <summary>
/// Unit Tests for Feature: View Flash Deal Details
/// Service: FlashDealService.GetByIdAsync
/// Function Code: UT-28
/// Sheet Name: UT_ViewFlashDealDetails
/// </summary>
public class ViewFlashDealDetailsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFlashDealRedisService> _redisMock = new();
    private readonly Mock<IFlashDealRepository> _dealRepoMock = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();
    private readonly Mock<ILogger<FlashDealService>> _loggerMock = new();
    private readonly FlashDealService _service;

    // Fixed Test Identifiers
    private readonly Guid _existingDealId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _nonExistentDealId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ViewFlashDealDetailsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FlashDealService(
            _redisMock.Object,
            _dealRepoMock.Object,
            _userAddressRepoMock.Object,
            _loggerMock.Object
        );
    }

    private FlashDeal CreateDatabaseDeal(bool withDealVariants = true, bool withShop = true)
    {
        var productId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        Shop? shop = withShop ? new Shop
        {
            Id = _shopId,
            Name = "SaveBite Bakery",
            AddressLine = "123 Le Loi Street",
            Latitude = 10.7769,
            Longitude = 106.7009,
            LogoUrl = "https://res.cloudinary.com/savebite/shop_logo.png"
        } : null;

        var product = new Product
        {
            Id = productId,
            Name = "Croissant Combo",
            Description = "Delicious crispy butter croissants",
            Category = new Category { Name = "Bakery" },
            Images = new List<ProductImage>
            {
                new() { ImageUrl = "https://res.cloudinary.com/savebite/croissant.jpg" }
            },
            Variants = new List<ProductVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Sku = "CROISSANT-01",
                    OriginalPrice = 50000,
                    DealPrice = 25000,
                    IsActive = true
                }
            }
        };

        var deal = new FlashDeal
        {
            Id = _existingDealId,
            ShopId = _shopId,
            Shop = shop,
            ProductId = productId,
            Product = product,
            SaleStartTime = DateTime.UtcNow.AddHours(-1),
            OrderEndTime = DateTime.UtcNow.AddHours(2),
            ShopClosingTime = DateTime.UtcNow.AddHours(3),
            Status = FlashDealStatus.OnSale,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        if (withDealVariants)
        {
            deal.Variants.Add(new FlashDealVariant
            {
                Id = Guid.NewGuid(),
                FlashDealId = _existingDealId,
                VariantId = product.Variants.First().Id,
                Variant = product.Variants.First(),
                OriginalPrice = 50000,
                DealPrice = 25000,
                DiscountPercent = 50,
                TotalQuantity = 20,
                SoldQuantity = 5,
                Status = FlashDealVariantStatus.Active
            });
        }

        return deal;
    }

    // =========================================================================
    // UTCID01: Xem chi tiết deal hợp lệ có đầy đủ FlashDealVariants -> Trả về chi tiết deal thành công (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetById_ExistingDealWithVariants_ReturnsDealDetails_Normal()
    {
        // Arrange
        var deal = CreateDatabaseDeal(withDealVariants: true, withShop: true);

        _dealRepoMock
            .Setup(r => r.GetActiveDealByIdAsync(_existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        // Act
        var result = await _service.GetByIdAsync(_existingDealId);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingDealId);
        result.ShopName.Should().Be("SaveBite Bakery");
        result.ProductName.Should().Be("Croissant Combo");
        result.MinDealPrice.Should().Be(25000);
        result.Variants.Should().HaveCount(1);
        result.Status.Should().Be(FlashDealStatus.OnSale);

        _dealRepoMock.Verify(r => r.GetActiveDealByIdAsync(_existingDealId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Xem chi tiết deal không có FlashDealVariants -> Trả về danh sách Variants rỗng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetById_FallbackToProductVariants_ReturnsDealDetails_Normal()
    {
        // Arrange
        var deal = CreateDatabaseDeal(withDealVariants: false, withShop: true);

        _dealRepoMock
            .Setup(r => r.GetActiveDealByIdAsync(_existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        // Act
        var result = await _service.GetByIdAsync(_existingDealId);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingDealId);
        result.Variants.Should().BeEmpty();

        _dealRepoMock.Verify(r => r.GetActiveDealByIdAsync(_existingDealId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Xem chi tiết deal khi thông tin Shop là null -> Vẫn trả về thông tin deal hợp lệ (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetById_DealWithoutShopInfo_ReturnsDealDetails_Normal()
    {
        // Arrange
        var deal = CreateDatabaseDeal(withDealVariants: true, withShop: false);

        _dealRepoMock
            .Setup(r => r.GetActiveDealByIdAsync(_existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        // Act
        var result = await _service.GetByIdAsync(_existingDealId);

        // Assert & Log
        PrintSuccess("UTCID03", result);
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingDealId);
        result.ShopName.Should().BeEmpty();

        _dealRepoMock.Verify(r => r.GetActiveDealByIdAsync(_existingDealId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID04: Deal không tồn tại trong hệ thống -> Ném AppException 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetById_DealNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _dealRepoMock
            .Setup(r => r.GetActiveDealByIdAsync(_nonExistentDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlashDeal?)null);

        // Act
        var act = () => _service.GetByIdAsync(_nonExistentDealId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID04", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Contain($"Flash deal with ID '{_nonExistentDealId}' was not found.");

        _dealRepoMock.Verify(r => r.GetActiveDealByIdAsync(_nonExistentDealId, It.IsAny<CancellationToken>()), Times.Once);
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

    private void PrintException(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: ERROR / REJECTED");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }
}
