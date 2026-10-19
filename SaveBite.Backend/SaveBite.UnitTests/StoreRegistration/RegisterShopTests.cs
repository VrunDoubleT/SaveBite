using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreRegistration;

/// <summary>
/// Unit Tests for Feature: Register Shop
/// Service: ShopApplicationService.CreateAsync
/// Function Code: UT-22
/// Sheet Name: UT_RegisterShop
/// </summary>
public class RegisterShopTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopApplicationRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly ShopApplicationService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public RegisterShopTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopApplicationService(
            _repositoryMock.Object,
            _cloudinaryMock.Object,
            new CreateShopApplicationRequestValidator(),
            new ResubmitShopApplicationRequestValidator(),
            new ShopApplicationDocumentsValidator(),
            new ShopApplicationLogoValidator()
        );
    }

    private static IFormFile CreateFakeFormFile(string fileName, string contentType, string content = "fake-file-content")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, Path.GetFileNameWithoutExtension(fileName), fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static CreateShopApplicationRequest CreateValidRequest() => new()
    {
        Name = "SaveBite Bistro",
        Description = "Fresh surplus bakery items",
        BusinessLicenseNo = "BL-987654",
        AddressLine = "123 Le Loi Street",
        Ward = "Ben Nghe",
        District = "District 1",
        City = "Ho Chi Minh City",
        Latitude = 10.7769,
        Longitude = 106.7009,
        OpeningTime = new TimeOnly(8, 0),
        ClosingTime = new TimeOnly(22, 0),
        BankName = "Vietcombank",
        BankAccountNumber = "987654321",
        BankAccountHolder = "NGUYEN VAN A",
        PayosClientId = "payos-client-id",
        PayosApiKey = "payos-api-key",
        PayosChecksumKey = "payos-checksum-key"
    };

    // =========================================================================
    // UTCID01: Đăng ký thành công với đầy đủ ảnh, tài liệu và thông tin hợp lệ (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_RegisterShop_FullInfo_WithImagesAndDocuments_ShouldSucceed_Normal()
    {
        // Arrange
        var request = CreateValidRequest();
        var logo = CreateFakeFormFile("logo.png", "image/png");
        var cover = CreateFakeFormFile("cover.jpg", "image/jpeg");
        var docFiles = new List<IFormFile>
        {
            CreateFakeFormFile("license.pdf", "application/pdf"),
            CreateFakeFormFile("foodsafety.pdf", "application/pdf")
        };
        var docTypes = new List<string> { "BusinessLicense", "FoodSafetyCertificate" };

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/savebite/image.png", "public_id_img"));

        _cloudinaryMock
            .Setup(c => c.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/savebite/doc.pdf", "public_id_doc"));

        // Act
        var result = await _service.CreateAsync(_userId, request, logo, cover, docFiles, docTypes);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Name.Should().Be("SaveBite Bistro");
        result.Status.Should().Be("Pending");
        result.RevisionNumber.Should().Be(1);
        result.LogoUrl.Should().Be("https://res.cloudinary.com/savebite/image.png");
        result.CoverImageUrl.Should().Be("https://res.cloudinary.com/savebite/image.png");
        result.Documents.Should().HaveCount(2);
        result.ReviewLogs.Should().ContainSingle(l => l.ToStatus == "Pending" && l.FromStatus == null);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<ShopApplication>(a =>
            a.ApplicantUserId == _userId &&
            a.Name == "SaveBite Bistro" &&
            a.Status == ShopApplicationStatus.Pending &&
            a.RevisionNumber == 1 &&
            a.Documents.Count == 2), It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Đăng ký thành công với thông tin tối thiểu (không logo, cover, documents, operating hours) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_RegisterShop_MinimalInfo_OptionalFieldsNull_ShouldSucceed_Normal()
    {
        // Arrange
        var request = CreateValidRequest();
        request.OpeningTime = null;
        request.ClosingTime = null;
        request.Description = null;
        request.BusinessLicenseNo = null;

        var logo = CreateFakeFormFile("logo.png", "image/png");

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(logo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/savebite/logo.png", "pub_logo"));

        // Act
        var result = await _service.CreateAsync(_userId, request, logo, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Name.Should().Be("SaveBite Bistro");
        result.Status.Should().Be("Pending");
        result.RevisionNumber.Should().Be(1);
        result.LogoUrl.Should().Be("https://res.cloudinary.com/savebite/logo.png");
        result.CoverImageUrl.Should().BeNull();
        result.Documents.Should().BeEmpty();

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Người dùng đã có đơn mở cửa hàng đang active -> Ném AppException Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_RegisterShop_ActiveApplicationExists_ThrowsConflict_Abnormal()
    {
        // Arrange
        var request = CreateValidRequest();
        var logo = CreateFakeFormFile("logo.png", "image/png");

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(_userId, request, logo, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID03", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("You already have an active shop application.");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Tên cửa hàng rỗng -> Validator từ chối ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_RegisterShop_EmptyShopName_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = ""; // Tên rỗng

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.CreateAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID04", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Name");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: Địa chỉ cửa hàng rỗng -> Validator từ chối ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_RegisterShop_EmptyAddressLine_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidRequest();
        request.AddressLine = ""; // Địa chỉ rỗng

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.CreateAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID05", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "AddressLine");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID06: Tọa độ Latitude ngoài khoảng [-90, 90] -> Validator ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID06_RegisterShop_LatitudeOutOfRange_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Latitude = 95.0; // Vượt quá 90

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.CreateAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID06", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Latitude");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID07: Giờ mở cửa >= giờ đóng cửa -> Validator ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID07_RegisterShop_OpeningTimeAfterClosingTime_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidRequest();
        request.OpeningTime = new TimeOnly(22, 0);
        request.ClosingTime = new TimeOnly(8, 0); // Opening >= Closing

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.CreateAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID07", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "OpeningTime" || e.PropertyName == "ClosingTime");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID08: Số tài khoản ngân hàng rỗng -> Validator ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID08_RegisterShop_EmptyBankAccountNumber_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidRequest();
        request.BankAccountNumber = ""; // Số tài khoản rỗng

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => _service.CreateAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID08", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "BankAccountNumber");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID09: Số lượng file và documentTypes không khớp -> Validator ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID09_RegisterShop_DocumentCountMismatch_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidRequest();
        var docFiles = new List<IFormFile>
        {
            CreateFakeFormFile("license.pdf", "application/pdf"),
            CreateFakeFormFile("safety.pdf", "application/pdf")
        };
        var docTypes = new List<string> { "BusinessLicense" }; // 2 files nhưng chỉ có 1 type

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var logo = CreateFakeFormFile("logo.png", "image/png");

        // Act
        var act = () => _service.CreateAsync(_userId, request, logo, null, docFiles, docTypes);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID09", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("Each uploaded document must have a document type"));

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID10: Số lượng tài liệu vượt quá 10 file -> Validator ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID10_RegisterShop_ExceedsMaxDocuments_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidRequest();
        var docFiles = new List<IFormFile>();
        var docTypes = new List<string>();
        for (int i = 0; i < 11; i++) // 11 files > max 10
        {
            docFiles.Add(CreateFakeFormFile($"doc_{i}.pdf", "application/pdf"));
            docTypes.Add("Other");
        }

        _repositoryMock
            .Setup(r => r.HasActiveApplicationAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var logo = CreateFakeFormFile("logo.png", "image/png");

        // Act
        var act = () => _service.CreateAsync(_userId, request, logo, null, docFiles, docTypes);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID10", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("You can upload up to 10 documents"));

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
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
