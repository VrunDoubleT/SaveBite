using System.Net;
using System.Text.Json;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.FlashDealManagement;

/// <summary>
/// Unit Tests for Feature: View Flash Deal Details for Shop
/// Service: FlashDealService.GetShopDealDetailAsync
/// Function Code: UT-77
/// Sheet Name: UT_ViewFlashDealDetailsForShop
/// </summary>
public class ViewFlashDealDetailsForShopTests
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
    private readonly Guid _staffUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _shopId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _otherShopId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _existingDealId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly Guid _nonExistentDealId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    public ViewFlashDealDetailsForShopTests(ITestOutputHelper output)
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

    private Shop CreateSampleShop(Guid id, Guid ownerId, string name = "SaveBite Bakery")
    {
        return new Shop
        {
            Id = id,
            OwnerUserId = ownerId,
            Name = name,
            AddressLine = "123 Le Loi Street",
            Latitude = 10.7769,
            Longitude = 106.7009,
            Status = ShopStatus.Active
        };
    }

    private FlashDeal CreateSampleDeal(Guid dealId, Guid shopId, string productName = "Banh Mi Pate Dac Biet")
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        return new FlashDeal
        {
            Id = dealId,
            ShopId = shopId,
            ProductId = productId,
            SaleStartTime = now.AddHours(-1),
            OrderEndTime = now.AddHours(2),
            ShopClosingTime = now.AddHours(3),
            Status = FlashDealStatus.OnSale,
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-1),
            Product = new Product
            {
                Id = productId,
                ShopId = shopId,
                Name = productName,
                Description = "Banh mi pate gion ngon",
                CategoryId = categoryId,
                Category = new Category { Id = categoryId, Name = "Banh Mi" },
                Images = new List<ProductImage>
                {
                    new() { Id = Guid.NewGuid(), ProductId = productId, ImageUrl = "https://res.cloudinary.com/savebite/banhmi.png" }
                }
            },
            Variants = new List<FlashDealVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FlashDealId = dealId,
                    VariantId = Guid.NewGuid(),
                    OriginalPrice = 30000m,
                    DealPrice = 15000m,
                    DiscountPercent = 50m,
                    TotalQuantity = 20,
                    ReservedQuantity = 5,
                    SoldQuantity = 10,
                    Status = FlashDealVariantStatus.Active
                }
            }
        };
    }

    // =========================================================================
    // UTCID01: Store Owner views deal detail with matching shopId (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetShopDealDetail_OwnerWithMatchingShopId_ReturnsDealDetail_Normal()
    {
        // Arrange
        var shop = CreateSampleShop(_shopId, _ownerUserId);
        var deal = CreateSampleDeal(_existingDealId, _shopId);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        // Act
        var result = await _service.GetShopDealDetailAsync(_ownerUserId, _existingDealId, _shopId);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingDealId);
        result.ProductName.Should().Be("Banh Mi Pate Dac Biet");
        result.Variants.Should().HaveCount(1);
    }

    // =========================================================================
    // UTCID02: Active Staff views deal detail without shopId (Auto-detects Shop) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetShopDealDetail_ActiveStaffWithoutShopId_AutoDetectsShop_ReturnsDealDetail_Normal()
    {
        // Arrange
        var shop = CreateSampleShop(_shopId, _ownerUserId);
        var staffMembership = new ShopStaff
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            UserId = _staffUserId,
            Status = ShopStaffStatus.Active,
            Shop = shop
        };
        var deal = CreateSampleDeal(_existingDealId, _shopId, "Ca Phe Muoi");

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        _staffRepoMock
            .Setup(r => r.GetShopsByStaffUserIdAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopStaff> { staffMembership });

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _existingDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        // Act
        var result = await _service.GetShopDealDetailAsync(_staffUserId, _existingDealId, null);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingDealId);
        result.ProductName.Should().Be("Ca Phe Muoi");
    }

    // =========================================================================
    // UTCID03: Deal does not exist in shop -> Throws NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetShopDealDetail_DealNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var shop = CreateSampleShop(_shopId, _ownerUserId);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealByIdAsync(_shopId, _nonExistentDealId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlashDeal?)null);

        // Act
        Func<Task> act = async () => await _service.GetShopDealDetailAsync(_ownerUserId, _nonExistentDealId, _shopId);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID03", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Flash deal not found.");
    }

    // =========================================================================
    // UTCID04: User attempts to view deal of another shop -> Throws Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetShopDealDetail_UserAccessingOtherShop_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var ownedShop = CreateSampleShop(_shopId, _ownerUserId);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ownedShop);

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(_otherShopId, _ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopStaff?)null);

        // Act
        Func<Task> act = async () => await _service.GetShopDealDetailAsync(_ownerUserId, _existingDealId, _otherShopId);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID04", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Which.Message.Should().Be("You do not have access to view this shop's flash deals.");
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
