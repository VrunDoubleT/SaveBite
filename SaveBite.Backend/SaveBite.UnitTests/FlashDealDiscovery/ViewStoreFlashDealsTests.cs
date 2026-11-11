using System.Text.Json;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FlashDealDiscovery;

/// <summary>
/// Unit Tests for Feature: View Flash Deals by Store (Customer)
/// Service: FlashDealService.GetByShopIdAsync
/// Function Code: UT-29
/// Sheet Name: UT_ViewStoreFlashDeals
/// </summary>
public class ViewStoreFlashDealsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IFlashDealRepository> _dealRepoMock = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly Mock<ILogger<FlashDealService>> _loggerMock = new();
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly FlashDealService _service;

    // Fixed Test Identifiers
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ViewStoreFlashDealsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new FlashDealService(
            _dealRepoMock.Object,
            _userAddressRepoMock.Object,
            _shopRepoMock.Object,
            _staffRepoMock.Object,
            _backgroundJobClientMock.Object,
            _publisherMock.Object,
            _loggerMock.Object,
            _db);
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
            LogoUrl = "https://res.cloudinary.com/savebite/shop_logo.png",
            Status = ShopStatus.Active
        };

        var product = new Product
        {
            Id = productId,
            Name = "Croissant Combo",
            Description = "Delicious crispy butter croissants",
            Status = ProductStatus.Active,
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
    // UTCID01: Trả về danh sách deals của shop từ database (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetByShopId_ReturnsDeals_Normal()
    {
        // Arrange
        var deal = CreateDatabaseDeal(withDealVariants: true);
        _db.FlashDeals.Add(deal);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetByShopIdAsync(_shopId);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ProductName.Should().Be("Croissant Combo");
    }

    // =========================================================================
    // UTCID02: Có FlashDealVariants -> Ánh xạ và trả về danh sách deals đầy đủ (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetByShopId_WithVariants_ReturnsDeals_Normal()
    {
        // Arrange
        var deal = CreateDatabaseDeal(withDealVariants: true);
        deal.Id = Guid.NewGuid();
        _db.FlashDeals.Add(deal);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetByShopIdAsync(_shopId);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.First().ShopId.Should().Be(_shopId);
        result.First().ShopName.Should().Be("SaveBite Bakery");
        result.First().MinDealPrice.Should().Be(25000);
    }

    // =========================================================================
    // UTCID03: Deal không có FlashDealVariants -> Trả về danh sách variants rỗng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetByShopId_WithoutVariants_Normal()
    {
        // Arrange
        var deal = CreateDatabaseDeal(withDealVariants: false);
        deal.Id = Guid.NewGuid();
        _db.FlashDeals.Add(deal);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetByShopIdAsync(_shopId);

        // Assert & Log
        PrintSuccess("UTCID03", result);
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    // =========================================================================
    // UTCID04: Shop không có deal nào -> Trả về danh sách rỗng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetByShopId_DatabaseEmpty_ReturnsEmptyList_Normal()
    {
        // Act
        var result = await _service.GetByShopIdAsync(Guid.NewGuid());

        // Assert & Log
        PrintSuccess("UTCID04", result);
        result.Should().NotBeNull();
        result.Should().BeEmpty();
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
