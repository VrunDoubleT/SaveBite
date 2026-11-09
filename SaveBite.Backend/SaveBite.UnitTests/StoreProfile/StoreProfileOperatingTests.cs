using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Controllers;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreProfile;

/// <summary>
/// Unit Tests for Feature: Store Profile & Operating Management
/// Service: ShopService (GetOwnerShopProfileAsync, UpdateBusinessInfoAsync, UpdateShopImagesAsync)
/// Validators: UpdateBusinessInfoRequestValidator, UpdateShopImagesRequestValidator
/// Security: ShopsController Owner Endpoints ([StoreOwnerAnyStatusAccess])
/// Test Cases: TC-SPO-01 to TC-SPO-25
/// </summary>
public class StoreProfileOperatingTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopRepository> _shopRepoMock = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepoMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly ShopService _shopService;

    private readonly UpdateBusinessInfoRequestValidator _businessInfoValidator = new();
    private readonly UpdateShopImagesRequestValidator _imagesValidator = new();

    // Fixed Test Identifiers
    private readonly Guid _ownerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public StoreProfileOperatingTests(ITestOutputHelper output)
    {
        _output = output;
        _shopService = new ShopService(
            _shopRepoMock.Object,
            _userAddressRepoMock.Object,
            _cloudinaryMock.Object,
            _publisherMock.Object);

        // Default review stats mock
        _shopRepoMock
            .Setup(r => r.GetShopReviewStatsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((4.5, 12, new Dictionary<int, int> { { 5, 8 }, { 4, 3 }, { 3, 1 } }));
    }

    private Shop CreateValidShop(
        string? description = "Store description offering surplus baked goods.",
        string? logoUrl = "https://res.cloudinary.com/savebite/logo.png",
        string? coverImageUrl = "https://res.cloudinary.com/savebite/cover.jpg")
    {
        return new Shop
        {
            Id = _shopId,
            OwnerUserId = _ownerUserId,
            Name = "SaveBite Bakery",
            Description = description,
            AddressLine = "123 Nguyen Hue Street",
            Ward = "Ben Nghe Ward",
            District = "District 1",
            City = "Ho Chi Minh City",
            Latitude = 10.7769,
            Longitude = 106.7009,
            LogoUrl = logoUrl,
            CoverImageUrl = coverImageUrl,
            OpeningTime = new TimeOnly(7, 0),
            ClosingTime = new TimeOnly(22, 0),
            Status = ShopStatus.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static IFormFile CreateMockFormFile(
        string fileName = "image.png",
        string contentType = "image/png",
        long length = 1024)
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.ContentType).Returns(contentType);
        fileMock.Setup(f => f.Length).Returns(length);
        return fileMock.Object;
    }

    // =========================================================================
    // SECTION 1: View Shop Profile (TC-SPO-01 to TC-SPO-05)
    // =========================================================================

    [Fact]
    public async Task TC_SPO_01_ViewShopProfile_WithCompleteInformation_ReturnsShopProfileResponseWith200OK()
    {
        // 1. Arrange
        var shop = CreateValidShop();
        _shopRepoMock
            .Setup(r => r.GetShopProfileByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        // 2. Act
        var result = await _shopService.GetOwnerShopProfileAsync(_ownerUserId);

        // 3. Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(shop.Id);
        result.Name.Should().Be("SaveBite Bakery");
        result.Description.Should().Be("Store description offering surplus baked goods.");
        result.AddressLine.Should().Be("123 Nguyen Hue Street");
        result.LogoUrl.Should().Be("https://res.cloudinary.com/savebite/logo.png");
        result.CoverImageUrl.Should().Be("https://res.cloudinary.com/savebite/cover.jpg");
        result.Status.Should().Be("Active");
        result.AverageRating.Should().Be(4.5);
        result.TotalReviews.Should().Be(12);

        _output.WriteLine($"[TC-SPO-01] Successfully retrieved full shop profile for shop Id {result.Id}");
    }

    [Fact]
    public async Task TC_SPO_02_ViewShopProfile_WhenDescriptionAndCoverImageEmpty_ReturnsProfileWithDefaultPlaceholders()
    {
        // 1. Arrange
        var shop = CreateValidShop(description: null, coverImageUrl: null);
        _shopRepoMock
            .Setup(r => r.GetShopProfileByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        // 2. Act
        var result = await _shopService.GetOwnerShopProfileAsync(_ownerUserId);

        // 3. Assert
        result.Should().NotBeNull();
        result.Description.Should().BeNull();
        result.CoverImageUrl.Should().BeNull();
        result.LogoUrl.Should().NotBeNull();

        _output.WriteLine("[TC-SPO-02] Shop profile returned successfully with empty optional fields.");
    }

    [Fact]
    public void TC_SPO_03_AccessPrivateShopProfile_AsUnauthorizedUser_GuardedByStoreOwnerPolicy()
    {
        // 1. Arrange & 2. Act
        var method = typeof(ShopsController).GetMethod(nameof(ShopsController.GetOwnerProfile));
        var attribute = method?.GetCustomAttribute<StoreOwnerAnyStatusAccessAttribute>();

        // 3. Assert
        attribute.Should().NotBeNull("GET /api/shops/me must be decorated with StoreOwnerAnyStatusAccessAttribute per BR-071.");
        attribute!.Policy.Should().Be(AuthorizationPolicies.StoreOwnerAnyStatus,
            "Only Store Owner role is authorized; Customer and Staff access must be denied with 403 Forbidden.");

        _output.WriteLine("[TC-SPO-03] Verified endpoint is strictly guarded by StoreOwnerAnyStatus policy.");
    }

    [Fact]
    public void TC_SPO_04_AccessPrivateShopProfile_WithoutLogin_BlockedByAuthorizationAttribute()
    {
        // 1. Arrange & 2. Act
        var method = typeof(ShopsController).GetMethod(nameof(ShopsController.GetOwnerProfile));
        var attribute = method?.GetCustomAttribute<StoreOwnerAnyStatusAccessAttribute>();

        // 3. Assert
        attribute.Should().NotBeNull("Unauthenticated guest access must be blocked with 401 Unauthorized.");
        attribute.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();

        _output.WriteLine("[TC-SPO-04] Verified unauthenticated guest access is blocked by AuthorizeAttribute.");
    }

    [Fact]
    public async Task TC_SPO_05_ViewShopProfile_WhenOwnerHasNoLinkedShop_Throws404NotFound()
    {
        // 1. Arrange
        _shopRepoMock
            .Setup(r => r.GetShopProfileByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // 2. Act
        var act = () => _shopService.GetOwnerShopProfileAsync(_ownerUserId);

        // 3. Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop not found.");

        _output.WriteLine("[TC-SPO-05] Correctly threw 404 NotFound 'Shop not found.' when shop record does not exist.");
    }

    // =========================================================================
    // SECTION 2: Edit Business Info (TC-SPO-06 to TC-SPO-17)
    // =========================================================================

    [Fact]
    public async Task TC_SPO_06_UpdateBusinessInfo_WithCompleteValidData_UpdatesAndReturnsProfileWith200OK()
    {
        // 1. Arrange
        var existingShop = CreateValidShop();
        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        _shopRepoMock
            .Setup(r => r.GetShopProfileByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        var request = new UpdateBusinessInfoRequest
        {
            Name = "SaveBite Flagship Bakery",
            Description = "Updated description with delicious organic bread.",
            AddressLine = "456 Le Loi Boulevard",
            Ward = "Ben Nghe",
            District = "District 1",
            City = "Ho Chi Minh City",
            Latitude = 10.7731,
            Longitude = 106.6983,
            OpeningTime = new TimeOnly(6, 30),
            ClosingTime = new TimeOnly(21, 30)
        };

        // 2. Act
        var result = await _shopService.UpdateBusinessInfoAsync(_ownerUserId, request);

        // 3. Assert
        _shopRepoMock.Verify(r => r.UpdateShopAsync(
            It.Is<Shop>(s =>
                s.Name == "SaveBite Flagship Bakery" &&
                s.AddressLine == "456 Le Loi Boulevard" &&
                s.Latitude == 10.7731 &&
                s.Longitude == 106.6983),
            It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Name.Should().Be("SaveBite Flagship Bakery");

        _output.WriteLine("[TC-SPO-06] Successfully updated shop business info with valid data.");
    }

    [Fact]
    public async Task TC_SPO_07_UpdateBusinessInfo_WithEmptyShopName_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var request = new UpdateBusinessInfoRequest
        {
            Name = "",
            AddressLine = "123 Valid Street",
            Latitude = 10.0,
            Longitude = 106.0
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Shop name is required.");

        _output.WriteLine("[TC-SPO-07] Validation caught empty shop name as expected.");
    }

    [Fact]
    public async Task TC_SPO_08_UpdateBusinessInfo_WithShopNameShorterThan2Chars_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var request = new UpdateBusinessInfoRequest
        {
            Name = "A",
            AddressLine = "123 Valid Street",
            Latitude = 10.0,
            Longitude = 106.0
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Shop name must contain at least 2 characters.");

        _output.WriteLine("[TC-SPO-08] Validation caught shop name shorter than 2 characters.");
    }

    [Fact]
    public async Task TC_SPO_09_UpdateBusinessInfo_WithShopNameExceeding200Chars_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var request = new UpdateBusinessInfoRequest
        {
            Name = new string('A', 201),
            AddressLine = "123 Valid Street",
            Latitude = 10.0,
            Longitude = 106.0
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Shop name must not exceed 200 characters.");

        _output.WriteLine("[TC-SPO-09] Validation caught shop name exceeding 200 characters.");
    }

    [Fact]
    public async Task TC_SPO_10_UpdateBusinessInfo_WithEmptyAddressLine_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var request = new UpdateBusinessInfoRequest
        {
            Name = "Valid Shop",
            AddressLine = "",
            Latitude = 10.0,
            Longitude = 106.0
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Address is required.");

        _output.WriteLine("[TC-SPO-10] Validation caught empty address line.");
    }

    [Fact]
    public async Task TC_SPO_11_UpdateBusinessInfo_WithAddressLineExceeding500Chars_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var request = new UpdateBusinessInfoRequest
        {
            Name = "Valid Shop",
            AddressLine = new string('X', 501),
            Latitude = 10.0,
            Longitude = 106.0
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Address must not exceed 500 characters.");

        _output.WriteLine("[TC-SPO-11] Validation caught address line exceeding 500 characters.");
    }

    [Fact]
    public async Task TC_SPO_12_UpdateBusinessInfo_WithLatitudeOutsideRange_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var request = new UpdateBusinessInfoRequest
        {
            Name = "Valid Shop",
            AddressLine = "123 Valid Address",
            Latitude = 95.0,
            Longitude = 106.0
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Latitude must be between -90 and 90.");

        _output.WriteLine("[TC-SPO-12] Validation caught out-of-range latitude 95.0.");
    }

    [Fact]
    public async Task TC_SPO_13_UpdateBusinessInfo_WithLongitudeOutsideRange_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var request = new UpdateBusinessInfoRequest
        {
            Name = "Valid Shop",
            AddressLine = "123 Valid Address",
            Latitude = 10.0,
            Longitude = 185.0
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Longitude must be between -180 and 180.");

        _output.WriteLine("[TC-SPO-13] Validation caught out-of-range longitude 185.0.");
    }

    [Fact]
    public async Task TC_SPO_14_UpdateBusinessInfo_WithInvalidOperatingHours_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var request = new UpdateBusinessInfoRequest
        {
            Name = "Valid Shop",
            AddressLine = "123 Valid Address",
            Latitude = 10.0,
            Longitude = 106.0,
            OpeningTime = new TimeOnly(20, 0),
            ClosingTime = new TimeOnly(8, 0)
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Closing time must be later than opening time.");

        _output.WriteLine("[TC-SPO-14] Validation caught closing time earlier than opening time per BR-072.");
    }

    [Fact]
    public async Task TC_SPO_15_UpdateBusinessInfo_WithDescriptionExceeding2000Chars_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var request = new UpdateBusinessInfoRequest
        {
            Name = "Valid Shop",
            AddressLine = "123 Valid Address",
            Latitude = 10.0,
            Longitude = 106.0,
            Description = new string('D', 2001)
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Description must not exceed 2000 characters.");

        _output.WriteLine("[TC-SPO-15] Validation caught description exceeding 2000 characters.");
    }

    [Fact]
    public async Task TC_SPO_16_UpdateBusinessInfo_WithoutChangingOptionalOperatingHours_SucceedsWith200OK()
    {
        // 1. Arrange
        var existingShop = CreateValidShop();
        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        _shopRepoMock
            .Setup(r => r.GetShopProfileByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        var request = new UpdateBusinessInfoRequest
        {
            Name = "SaveBite Bakery 24/7",
            AddressLine = "789 Tran Hung Dao",
            Latitude = 10.75,
            Longitude = 106.68,
            OpeningTime = null,
            ClosingTime = null
        };

        // 2. Act
        var validationResult = await _businessInfoValidator.ValidateAsync(request);
        var result = await _shopService.UpdateBusinessInfoAsync(_ownerUserId, request);

        // 3. Assert
        validationResult.IsValid.Should().BeTrue("Leaving opening/closing times null is valid and should not trigger validation error.");
        result.Should().NotBeNull();
        _shopRepoMock.Verify(r => r.UpdateShopAsync(It.IsAny<Shop>(), It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine("[TC-SPO-16] Business info updated successfully without changing optional operating hours.");
    }

    [Fact]
    public void TC_SPO_17_UpdateBusinessInfo_AsUnauthorizedNonOwner_GuardedByStoreOwnerPolicy()
    {
        // 1. Arrange & 2. Act
        var method = typeof(ShopsController).GetMethod(nameof(ShopsController.UpdateBusinessInfo));
        var attribute = method?.GetCustomAttribute<StoreOwnerAnyStatusAccessAttribute>();

        // 3. Assert
        attribute.Should().NotBeNull("PUT /api/shops/me/business-info must be guarded by StoreOwnerAnyStatusAccessAttribute per BR-071.");
        attribute!.Policy.Should().Be(AuthorizationPolicies.StoreOwnerAnyStatus);

        _output.WriteLine("[TC-SPO-17] Verified business info endpoint is guarded by StoreOwnerAnyStatus policy.");
    }

    // =========================================================================
    // SECTION 3: Edit Shop Images and Logo (TC-SPO-18 to TC-SPO-25)
    // =========================================================================

    [Fact]
    public async Task TC_SPO_18_UpdateShopImages_WithValidLogo_UploadsToCloudinaryAndReturns200OK()
    {
        // 1. Arrange
        var existingShop = CreateValidShop();
        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        _shopRepoMock
            .Setup(r => r.GetShopProfileByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        var logoFile = CreateMockFormFile("new_logo.png", "image/png", 2 * 1024 * 1024);
        var request = new UpdateShopImagesRequest { Logo = logoFile };

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(logoFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/savebite/new_logo.png", "logo_public_id"));

        // 2. Act
        var result = await _shopService.UpdateShopImagesAsync(_ownerUserId, request);

        // 3. Assert
        _cloudinaryMock.Verify(c => c.UploadImageAsync(logoFile, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.UpdateShopAsync(It.Is<Shop>(s => s.LogoUrl == "https://res.cloudinary.com/savebite/new_logo.png"), It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();

        _output.WriteLine("[TC-SPO-18] Shop logo uploaded and updated successfully.");
    }

    [Fact]
    public async Task TC_SPO_19_UpdateShopImages_WithValidCoverImage_UploadsToCloudinaryAndReturns200OK()
    {
        // 1. Arrange
        var existingShop = CreateValidShop();
        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        _shopRepoMock
            .Setup(r => r.GetShopProfileByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        var coverFile = CreateMockFormFile("new_cover.jpg", "image/jpeg", 3 * 1024 * 1024);
        var request = new UpdateShopImagesRequest { CoverImage = coverFile };

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(coverFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/savebite/new_cover.jpg", "cover_public_id"));

        // 2. Act
        var result = await _shopService.UpdateShopImagesAsync(_ownerUserId, request);

        // 3. Assert
        _cloudinaryMock.Verify(c => c.UploadImageAsync(coverFile, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.UpdateShopAsync(It.Is<Shop>(s => s.CoverImageUrl == "https://res.cloudinary.com/savebite/new_cover.jpg"), It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();

        _output.WriteLine("[TC-SPO-19] Shop cover banner uploaded and updated successfully.");
    }

    [Fact]
    public async Task TC_SPO_20_UpdateShopImages_WithBothLogoAndCoverImage_UploadsBothSuccessfully()
    {
        // 1. Arrange
        var existingShop = CreateValidShop();
        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        _shopRepoMock
            .Setup(r => r.GetShopProfileByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingShop);

        var logoFile = CreateMockFormFile("both_logo.png", "image/png", 1024 * 1024);
        var coverFile = CreateMockFormFile("both_cover.webp", "image/webp", 2 * 1024 * 1024);
        var request = new UpdateShopImagesRequest { Logo = logoFile, CoverImage = coverFile };

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(logoFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/savebite/both_logo.png", "logo_id"));

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(coverFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/savebite/both_cover.webp", "cover_id"));

        // 2. Act
        var result = await _shopService.UpdateShopImagesAsync(_ownerUserId, request);

        // 3. Assert
        _cloudinaryMock.Verify(c => c.UploadImageAsync(logoFile, It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryMock.Verify(c => c.UploadImageAsync(coverFile, It.IsAny<CancellationToken>()), Times.Once);
        _shopRepoMock.Verify(r => r.UpdateShopAsync(
            It.Is<Shop>(s =>
                s.LogoUrl == "https://res.cloudinary.com/savebite/both_logo.png" &&
                s.CoverImageUrl == "https://res.cloudinary.com/savebite/both_cover.webp"),
            It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();

        _output.WriteLine("[TC-SPO-20] Both shop logo and cover banner uploaded and updated simultaneously.");
    }

    [Fact]
    public async Task TC_SPO_21_UploadShopLogo_WithUnsupportedFileFormat_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var unsupportedFile = CreateMockFormFile("document.pdf", "application/pdf", 1024 * 1024);
        var request = new UpdateShopImagesRequest { Logo = unsupportedFile };

        // 2. Act
        var validationResult = await _imagesValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Shop logo must be a JPG, PNG or WEBP image.");

        _output.WriteLine("[TC-SPO-21] Validation caught unsupported logo format application/pdf.");
    }

    [Fact]
    public async Task TC_SPO_22_UploadShopLogo_Exceeding5MB_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var oversizedFile = CreateMockFormFile("huge_logo.png", "image/png", 6 * 1024 * 1024);
        var request = new UpdateShopImagesRequest { Logo = oversizedFile };

        // 2. Act
        var validationResult = await _imagesValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Shop logo must not exceed 5 MB.");

        _output.WriteLine("[TC-SPO-22] Validation caught oversized shop logo (> 5 MB).");
    }

    [Fact]
    public async Task TC_SPO_23_UploadCoverBanner_Exceeding5MB_FailsValidationWith400BadRequest()
    {
        // 1. Arrange
        var oversizedCover = CreateMockFormFile("huge_cover.jpg", "image/jpeg", 6 * 1024 * 1024);
        var request = new UpdateShopImagesRequest { CoverImage = oversizedCover };

        // 2. Act
        var validationResult = await _imagesValidator.ValidateAsync(request);

        // 3. Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Cover image must not exceed 5 MB.");

        _output.WriteLine("[TC-SPO-23] Validation caught oversized cover banner (> 5 MB).");
    }

    [Fact]
    public async Task TC_SPO_24_UpdateShopImages_WithoutLogoWhenShopHasNoExistingLogo_Throws400BadRequest()
    {
        // 1. Arrange
        var shopWithoutLogo = CreateValidShop(logoUrl: null);
        _shopRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shopWithoutLogo);

        var request = new UpdateShopImagesRequest { Logo = null };

        // 2. Act
        var act = () => _shopService.UpdateShopImagesAsync(_ownerUserId, request);

        // 3. Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Shop logo is required.");

        _output.WriteLine("[TC-SPO-24] Correctly rejected update when shop has no logo and none is provided in request.");
    }

    [Fact]
    public void TC_SPO_25_UpdateShopImages_AsUnauthorizedNonOwner_GuardedByStoreOwnerPolicy()
    {
        // 1. Arrange & 2. Act
        var method = typeof(ShopsController).GetMethod(nameof(ShopsController.UpdateShopImages));
        var attribute = method?.GetCustomAttribute<StoreOwnerAnyStatusAccessAttribute>();

        // 3. Assert
        attribute.Should().NotBeNull("PUT /api/shops/me/images must be guarded by StoreOwnerAnyStatusAccessAttribute per BR-071.");
        attribute!.Policy.Should().Be(AuthorizationPolicies.StoreOwnerAnyStatus);

        _output.WriteLine("[TC-SPO-25] Verified shop images update endpoint is guarded by StoreOwnerAnyStatus policy.");
    }
}
