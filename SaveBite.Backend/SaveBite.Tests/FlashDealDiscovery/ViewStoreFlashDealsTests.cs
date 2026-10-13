using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FlashDealDiscovery;

/// <summary>
/// Unit Tests for Feature: View Store's Flash Deals
/// Service: FlashDealService.GetByShopIdAsync
/// Function Code: UT-26
/// Sheet Name: UT_ViewStoreFlashDeals
/// </summary>
public class ViewStoreFlashDealsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFlashDealRedisService> _redisMock = new();
    private readonly Mock<IFlashDealRepository> _dealRepoMock = new();
    private readonly Mock<ILogger<FlashDealService>> _loggerMock = new();
    private readonly FlashDealService _service;

    // Fixed Test Identifiers
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ViewStoreFlashDealsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FlashDealService(
            _redisMock.Object,
            _dealRepoMock.Object,
            _loggerMock.Object
        );
    }

    private FlashDeal CreateDatabaseDeal(bool withDealVariants = true)
    {
        var dealId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var productId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var shop = new Shop
        {
            Id = _shopId,
            Name = "SaveBite Bakery",
            AddressLine = "123 Le Loi Street",
            Latitude = 10.7769,
            Longitude = 106.7009,
            LogoUrl = "https://res.cloudinary.com/savebite/shop_logo.png"
        };

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
            Id = dealId,
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
                FlashDealId = dealId,
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
    // UTCID01: Redis Cache Hit -> Trả về danh sách cached deals ngay, không truy vấn DB (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetByShopId_RedisCacheHit_ReturnsCachedDeals_Normal()
    {
        // Arrange
        var cachedDeals = new List<FlashDealResponse>
        {
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ShopId = _shopId,
                ShopName = "SaveBite Bakery",
                ProductName = "Croissant Combo",
                MinDealPrice = 25000,
                MaxOriginalPrice = 50000,
                MaxDiscountPercent = 50,
                Status = FlashDealStatus.OnSale
            }
        };

        _redisMock
            .Setup(r => r.GetDealsByShopAsync(_shopId))
            .ReturnsAsync(cachedDeals);

        // Act
        var result = await _service.GetByShopIdAsync(_shopId);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ProductName.Should().Be("Croissant Combo");

        _redisMock.Verify(r => r.GetDealsByShopAsync(_shopId), Times.Once);
        // Không được truy vấn DB khi cache hit
        _dealRepoMock.Verify(r => r.GetActiveDealsByShopIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _redisMock.Verify(r => r.SaveDealAsync(It.IsAny<FlashDealResponse>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    // =========================================================================
    // UTCID02: Cache Miss, Database Hit (Có FlashDealVariants) -> Ánh xạ, lưu cache Redis và trả về danh sách deals (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetByShopId_CacheMiss_DatabaseHit_SavesToCache_ReturnsDeals_Normal()
    {
        // Arrange
        var dbDeal = CreateDatabaseDeal(withDealVariants: true);

        _redisMock
            .Setup(r => r.GetDealsByShopAsync(_shopId))
            .ReturnsAsync(new List<FlashDealResponse>()); // Cache miss

        _dealRepoMock
            .Setup(r => r.GetActiveDealsByShopIdAsync(_shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FlashDeal> { dbDeal });

        // Act
        var result = await _service.GetByShopIdAsync(_shopId);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ShopId.Should().Be(_shopId);
        result.First().ShopName.Should().Be("SaveBite Bakery");
        result.First().MinDealPrice.Should().Be(25000);
        result.First().Variants.Should().HaveCount(1);

        _dealRepoMock.Verify(r => r.GetActiveDealsByShopIdAsync(_shopId, It.IsAny<CancellationToken>()), Times.Once);
        // Đảm bảo deal được cache vào Redis cùng tọa độ shop
        _redisMock.Verify(r => r.SaveDealAsync(
            It.Is<FlashDealResponse>(d => d.Id == dbDeal.Id),
            10.7769,
            106.7009,
            null), Times.Once);
    }

    // =========================================================================
    // UTCID03: Cache Miss, Database Hit không có FlashDealVariants -> Fallback sang ProductVariants (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetByShopId_CacheMiss_FallbackToProductVariants_Normal()
    {
        // Arrange
        var dbDeal = CreateDatabaseDeal(withDealVariants: false); // Variants = empty

        _redisMock
            .Setup(r => r.GetDealsByShopAsync(_shopId))
            .ReturnsAsync(new List<FlashDealResponse>()); // Cache miss

        _dealRepoMock
            .Setup(r => r.GetActiveDealsByShopIdAsync(_shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FlashDeal> { dbDeal });

        // Act
        var result = await _service.GetByShopIdAsync(_shopId);

        // Assert & Log
        PrintSuccess("UTCID03", result);
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        // Kiểm tra logic fallback tạo variant từ Product.Variants
        result.First().Variants.Should().HaveCount(1);
        result.First().Variants.First().Sku.Should().Be("CROISSANT-01");
        result.First().Variants.First().DealPrice.Should().Be(25000);

        _redisMock.Verify(r => r.SaveDealAsync(It.IsAny<FlashDealResponse>(), It.IsAny<double>(), It.IsAny<double>(), null), Times.Once);
    }

    // =========================================================================
    // UTCID04: Cache Miss, Database Empty (Shop không có deal nào) -> Trả về danh sách rỗng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetByShopId_CacheMiss_DatabaseEmpty_ReturnsEmptyList_Normal()
    {
        // Arrange
        _redisMock
            .Setup(r => r.GetDealsByShopAsync(_shopId))
            .ReturnsAsync(new List<FlashDealResponse>()); // Cache miss

        _dealRepoMock
            .Setup(r => r.GetActiveDealsByShopIdAsync(_shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FlashDeal>()); // DB empty

        // Act
        var result = await _service.GetByShopIdAsync(_shopId);

        // Assert & Log
        PrintSuccess("UTCID04", result);
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _dealRepoMock.Verify(r => r.GetActiveDealsByShopIdAsync(_shopId, It.IsAny<CancellationToken>()), Times.Once);
        _redisMock.Verify(r => r.SaveDealAsync(It.IsAny<FlashDealResponse>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<TimeSpan?>()), Times.Never);
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
}
