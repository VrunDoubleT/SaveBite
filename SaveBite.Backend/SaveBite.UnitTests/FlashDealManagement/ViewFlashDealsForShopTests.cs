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
/// Unit Tests for Feature: View Flash Deals for Shop
/// Service: FlashDealService.GetShopDealsAsync
/// Function Code: UT-76
/// Sheet Name: UT_ViewFlashDealsForShop
/// Business Rules: 
/// - Only the Store Owner or an active Staff member of a Shop may view that Shop's flash deals.
/// - Staff members with Suspended status or users without shop association cannot access flash deals.
/// - Unsold and active deals are displayed with price, discount, and inventory counters.
/// </summary>
public class ViewFlashDealsForShopTests
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
    private readonly Guid _unauthorizedUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private readonly Guid _shopId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _otherShopId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public ViewFlashDealsForShopTests(ITestOutputHelper output)
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

    private FlashDeal CreateSampleDeal(Guid shopId, string productName = "Banh Mi Pate Dac Biet", FlashDealStatus status = FlashDealStatus.OnSale)
    {
        var dealId = Guid.NewGuid();
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
            Status = status,
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-1),
            Product = new Product
            {
                Id = productId,
                ShopId = shopId,
                Name = productName,
                Description = "Banh mi gion voi pate thom ngon",
                CategoryId = categoryId,
                Category = new Category { Id = categoryId, Name = "Banh mi" },
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
    // UTCID01: Chu cua hang (Store Owner) xem danh sach Flash Deals cua cua hang minh so huu (khong can truyen ShopId) (Normal - N)
    // Business Rule: Chu shop co quyen xem danh sach flash deals cua shop minh, he thong tu nhan dien shop so huu
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetShopDeals_OwnerWithoutShopId_ReturnsShopDeals_Normal()
    {
        // Arrange
        var request = new ShopFlashDealQueryRequest { ShopId = null };
        var shop = CreateSampleShop(_shopId, _ownerUserId);
        var deal = CreateSampleDeal(_shopId, "Banh Mi Pate Dac Biet");
        var pagedDeals = new PagedResult<FlashDeal>
        {
            Items = new List<FlashDeal> { deal },
            TotalItems = 1,
            Page = 1,
            PageSize = 10
        };

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealsAsync(_shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedDeals);

        // Act
        var result = await _service.GetShopDealsAsync(_ownerUserId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(deal.Id);
        result.Items[0].ProductName.Should().Be("Banh Mi Pate Dac Biet");
        result.Items[0].MinDealPrice.Should().Be(15000m);
        result.Items[0].MaxDiscountPercent.Should().Be(50m);
        result.Items[0].TotalQuantity.Should().Be(20);
        result.Items[0].SoldQuantity.Should().Be(10);
    }

    // =========================================================================
    // UTCID02: Chu cua hang (Store Owner) xem danh sach Flash Deals khi truyen dung ShopId cua cua hang minh (Normal - N)
    // Business Rule: Chu shop xac thuc quyen so huu thanh cong qua ShopId va xem duoc danh sach deal cua shop do
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetShopDeals_OwnerWithMatchingShopId_ReturnsShopDeals_Normal()
    {
        // Arrange
        var request = new ShopFlashDealQueryRequest { ShopId = _shopId };
        var shop = CreateSampleShop(_shopId, _ownerUserId);
        var deal = CreateSampleDeal(_shopId, "Ca Phe Muoi");
        var pagedDeals = new PagedResult<FlashDeal>
        {
            Items = new List<FlashDeal> { deal },
            TotalItems = 1,
            Page = 1,
            PageSize = 10
        };

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealsAsync(_shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedDeals);

        // Act
        var result = await _service.GetShopDealsAsync(_ownerUserId, request);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items[0].ProductName.Should().Be("Ca Phe Muoi");
    }

    // =========================================================================
    // UTCID03: Nhan vien dang hoat dong (Active Staff) xem danh sach Flash Deals khi truyen dung ShopId duoc phan cong (Normal - N)
    // Business Rule: Nhan vien co trang thai Active tai cua hang duoc phep xem danh sach flash deals cua cua hang do
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetShopDeals_ActiveStaffWithShopId_ReturnsShopDeals_Normal()
    {
        // Arrange
        var request = new ShopFlashDealQueryRequest { ShopId = _shopId };
        var shop = CreateSampleShop(_shopId, _ownerUserId);
        var staff = new ShopStaff
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            UserId = _staffUserId,
            Status = ShopStaffStatus.Active,
            Shop = shop
        };
        var deal = CreateSampleDeal(_shopId);
        var pagedDeals = new PagedResult<FlashDeal>
        {
            Items = new List<FlashDeal> { deal },
            TotalItems = 1,
            Page = 1,
            PageSize = 10
        };

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        _dealRepoMock
            .Setup(r => r.GetShopDealsAsync(_shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedDeals);

        // Act
        var result = await _service.GetShopDealsAsync(_staffUserId, request);

        // Assert & Log
        PrintSuccess("UTCID03", result);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    // =========================================================================
    // UTCID04: Nhan vien dang hoat dong (Active Staff) xem danh sach Flash Deals khong truyen ShopId (Normal - N)
    // Business Rule: Nhan vien khong truyen ShopId thi he thong tu dong nhan dien cua hang nhan vien dang lam viec
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetShopDeals_ActiveStaffWithoutShopId_AutoDetectsShop_ReturnsShopDeals_Normal()
    {
        // Arrange
        var request = new ShopFlashDealQueryRequest { ShopId = null };
        var shop = CreateSampleShop(_shopId, _ownerUserId);
        var staffMembership = new ShopStaff
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            UserId = _staffUserId,
            Status = ShopStaffStatus.Active,
            Shop = shop
        };
        var deal = CreateSampleDeal(_shopId);
        var pagedDeals = new PagedResult<FlashDeal>
        {
            Items = new List<FlashDeal> { deal },
            TotalItems = 1,
            Page = 1,
            PageSize = 10
        };

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        _staffRepoMock
            .Setup(r => r.GetShopsByStaffUserIdAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopStaff> { staffMembership });

        _dealRepoMock
            .Setup(r => r.GetShopDealsAsync(_shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedDeals);

        // Act
        var result = await _service.GetShopDealsAsync(_staffUserId, request);

        // Assert & Log
        PrintSuccess("UTCID04", result);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items[0].Id.Should().Be(deal.Id);
    }

    // =========================================================================
    // UTCID05: Loc danh sach Flash Deals theo trang thai dang ban (OnSale) va khoang thoi gian tao (Normal - N)
    // Business Rule: He thong ho tro loc deal theo trang thai FlashDealStatus va moc thoi gian FromDate/ToDate
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetShopDeals_FilterByStatusAndDateRange_ReturnsMatchingDeals_Normal()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var request = new ShopFlashDealQueryRequest
        {
            ShopId = _shopId,
            Status = FlashDealStatus.OnSale,
            FromDate = fromDate,
            ToDate = toDate
        };

        var shop = CreateSampleShop(_shopId, _ownerUserId);
        var deal = CreateSampleDeal(_shopId, status: FlashDealStatus.OnSale);
        var pagedDeals = new PagedResult<FlashDeal>
        {
            Items = new List<FlashDeal> { deal },
            TotalItems = 1,
            Page = 1,
            PageSize = 10
        };

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealsAsync(_shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedDeals);

        // Act
        var result = await _service.GetShopDealsAsync(_ownerUserId, request);

        // Assert & Log
        PrintSuccess("UTCID05", result);
        result.Should().NotBeNull();
        result.Items[0].Status.Should().Be(FlashDealStatus.OnSale);
        _dealRepoMock.Verify(r => r.GetShopDealsAsync(_shopId, It.Is<ShopFlashDealQueryRequest>(req =>
            req.Status == FlashDealStatus.OnSale && req.FromDate == fromDate && req.ToDate == toDate), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID06: Cua hang chua co Flash Deal nao trong CSDL (Normal - N)
    // Business Rule: Tra ve danh sach rong neu cua hang chua tao flash deal nao hoac khong co deal phu hop
    // =========================================================================
    [Fact]
    public async Task UTCID06_GetShopDeals_NoDealsExist_ReturnsEmptyList_Normal()
    {
        // Arrange
        var request = new ShopFlashDealQueryRequest { ShopId = _shopId };
        var shop = CreateSampleShop(_shopId, _ownerUserId);
        var emptyPagedDeals = new PagedResult<FlashDeal>
        {
            Items = new List<FlashDeal>(),
            TotalItems = 0,
            Page = 1,
            PageSize = 10
        };

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _dealRepoMock
            .Setup(r => r.GetShopDealsAsync(_shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPagedDeals);

        // Act
        var result = await _service.GetShopDealsAsync(_ownerUserId, request);

        // Assert & Log
        PrintSuccess("UTCID06", result);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // =========================================================================
    // UTCID07: Nguoi dung khong so huu cua hang va khong phai nhan vien cua bat ky cua hang nao (Abnormal - A)
    // Business Rule: Chi Store Owner hoac Active Staff moi duoc truy cap, nguoi dung khong thuoc shop nao bi tu choi 403 Forbidden
    // =========================================================================
    [Fact]
    public async Task UTCID07_GetShopDeals_UserDoesNotOwnOrWorkAtAnyShop_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var request = new ShopFlashDealQueryRequest { ShopId = null };

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_unauthorizedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        _staffRepoMock
            .Setup(r => r.GetShopsByStaffUserIdAsync(_unauthorizedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopStaff>());

        // Act
        Func<Task> act = async () => await _service.GetShopDealsAsync(_unauthorizedUserId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID07", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Which.Message.Should().Be("You do not own or work at any shop.");
    }

    // =========================================================================
    // UTCID08: Nguoi dung co tinh truy cap xem Flash Deals cua cua hang khac ma minh khong co quyen (Abnormal - A)
    // Business Rule: Khong duoc phep truy cap xem flash deals cua shop khac khi khong co quyen so huu hay lam viec tai shop do
    // =========================================================================
    [Fact]
    public async Task UTCID08_GetShopDeals_UserAccessingOtherShopDeals_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var request = new ShopFlashDealQueryRequest { ShopId = _otherShopId };
        var ownedShop = CreateSampleShop(_shopId, _ownerUserId);

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ownedShop);

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(_otherShopId, _ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopStaff?)null);

        // Act
        Func<Task> act = async () => await _service.GetShopDealsAsync(_ownerUserId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID08", (int)exception.Which.StatusCode, exception.Which.Message);
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Which.Message.Should().Be("You do not have access to view this shop's flash deals.");
    }

    // =========================================================================
    // UTCID09: Nhan vien co trang thai bi dinh chi (Suspended Staff) co tinh xem Flash Deals cua shop (Abnormal - A)
    // Business Rule: Nhan vien khong o trang thai Active (vi du: Suspended) khong co quyen xem Flash Deals cua cua hang
    // =========================================================================
    [Fact]
    public async Task UTCID09_GetShopDeals_SuspendedStaffMember_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var request = new ShopFlashDealQueryRequest { ShopId = _shopId };
        var suspendedStaff = new ShopStaff
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            UserId = _staffUserId,
            Status = ShopStaffStatus.Suspended
        };

        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedStaff);

        // Act
        Func<Task> act = async () => await _service.GetShopDealsAsync(_staffUserId, request);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        PrintError("UTCID09", (int)exception.Which.StatusCode, exception.Which.Message);
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
