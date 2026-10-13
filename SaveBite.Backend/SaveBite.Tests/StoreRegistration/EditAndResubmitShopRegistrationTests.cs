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
/// Unit Tests for Feature: Edit and Resubmit Shop Registration
/// Service: ShopApplicationService.ResubmitAsync
/// Function Code: UT-24
/// Sheet Name: UT_EditAndResubmitShopRegistration
/// </summary>
public class EditAndResubmitShopRegistrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopApplicationRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly ShopApplicationService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _applicationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public EditAndResubmitShopRegistrationTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopApplicationService(
            _repositoryMock.Object,
            _cloudinaryMock.Object,
            new CreateShopApplicationRequestValidator(),
            new ResubmitShopApplicationRequestValidator(),
            new ShopApplicationDocumentsValidator()
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

    private ResubmitShopApplicationRequest CreateValidResubmitRequest() => new()
    {
        ApplicationId = _applicationId,
        Name = "SaveBite Bistro Updated",
        Description = "Updated surplus food store",
        BusinessLicenseNo = "BL-999999",
        AddressLine = "456 Nguyen Hue Boulevard",
        Ward = "Ben Nghe",
        District = "District 1",
        City = "Ho Chi Minh City",
        Latitude = 10.7745,
        Longitude = 106.7025,
        OpeningTime = new TimeOnly(7, 30),
        ClosingTime = new TimeOnly(21, 30),
        BankName = "Techcombank",
        BankAccountNumber = "1234567890",
        BankAccountHolder = "NGUYEN VAN A",
        PayosClientId = "new-client-id",
        PayosApiKey = "new-api-key",
        PayosChecksumKey = "new-checksum-key"
    };

    private ShopApplication CreateExistingApplication(ShopApplicationStatus status, int revisionNumber = 1)
    {
        var now = DateTime.UtcNow;
        var app = new ShopApplication
        {
            Id = _applicationId,
            ApplicantUserId = _userId,
            Name = "SaveBite Bistro",
            Description = "Old description",
            BusinessLicenseNo = "BL-111111",
            AddressLine = "123 Le Loi Street",
            Latitude = 10.7769,
            Longitude = 106.7009,
            OpeningTime = new TimeOnly(8, 0),
            ClosingTime = new TimeOnly(22, 0),
            BankName = "Vietcombank",
            BankAccountNumber = "987654321",
            BankAccountHolder = "NGUYEN VAN A",
            PayosClientId = "old-client-id",
            PayosApiKey = "old-api-key",
            PayosChecksumKey = "old-checksum-key",
            Status = status,
            RevisionNumber = revisionNumber,
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-2)
        };

        // Tài liệu ban đầu hợp lệ
        app.Documents.Add(new ShopApplicationDocument
        {
            Id = Guid.NewGuid(),
            ApplicationId = _applicationId,
            DocumentType = ShopDocumentType.BusinessLicense,
            FileUrl = "https://res.cloudinary.com/savebite/license_old.pdf",
            OriginalFileName = "license_old.pdf",
            ContentType = "application/pdf",
            RevisionNumber = revisionNumber,
            IsCurrent = true,
            UploadedAt = now.AddDays(-5)
        });
        app.Documents.Add(new ShopApplicationDocument
        {
            Id = Guid.NewGuid(),
            ApplicationId = _applicationId,
            DocumentType = ShopDocumentType.FoodSafetyCertificate,
            FileUrl = "https://res.cloudinary.com/savebite/foodsafety_old.pdf",
            OriginalFileName = "foodsafety_old.pdf",
            ContentType = "application/pdf",
            RevisionNumber = revisionNumber,
            IsCurrent = true,
            UploadedAt = now.AddDays(-5)
        });

        return app;
    }

    // =========================================================================
    // UTCID01: Resubmit từ đơn Rejected, thay thế tài liệu mới -> Thành công, Status: Pending, Revision tăng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_ResubmitShop_FromRejected_WithNewDocuments_ShouldSucceed_Normal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();
        var existingApp = CreateExistingApplication(ShopApplicationStatus.Rejected, revisionNumber: 1);
        
        var newLicense = CreateFakeFormFile("license_new.pdf", "application/pdf");
        var docs = new List<IFormFile> { newLicense };
        var docTypes = new List<string> { "BusinessLicense" };

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_applicationId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingApp);

        _cloudinaryMock
            .Setup(c => c.UploadDocumentAsync(newLicense, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/savebite/license_new.pdf", "pub_doc_new"));

        // Act
        var result = await _service.ResubmitAsync(_userId, request, null, null, docs, docTypes);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Name.Should().Be("SaveBite Bistro Updated");
        result.Status.Should().Be("Pending");
        result.RevisionNumber.Should().Be(2);

        // Tài liệu cũ bị đánh dấu IsCurrent = false, tài liệu mới nộp có IsCurrent = true
        var oldLicense = existingApp.Documents.First(d => d.OriginalFileName == "license_old.pdf");
        oldLicense.IsCurrent.Should().BeFalse();
        var currentDocs = result.Documents.Where(d => d.IsCurrent).ToList();
        currentDocs.Should().HaveCount(2); // New BusinessLicense + Existing FoodSafety

        existingApp.ReviewLogs.Should().Contain(l => l.FromStatus == ShopApplicationStatus.Rejected && l.ToStatus == ShopApplicationStatus.Pending);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Resubmit từ đơn Cancelled, giữ lại PayOS cũ và tài liệu cũ -> Thành công, Status: Pending (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_ResubmitShop_FromCancelled_KeepExistingPayosAndDocuments_ShouldSucceed_Normal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();
        request.PayosClientId = ""; // Không đổi PayOS
        request.PayosApiKey = "";
        request.PayosChecksumKey = "";

        var existingApp = CreateExistingApplication(ShopApplicationStatus.Cancelled, revisionNumber: 1);

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_applicationId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingApp);

        // Act
        var result = await _service.ResubmitAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Status.Should().Be("Pending");
        result.RevisionNumber.Should().Be(2);
        existingApp.PayosClientId.Should().Be("old-client-id"); // Giữ nguyên PayOS cũ
        existingApp.ReviewLogs.Should().Contain(l => l.FromStatus == ShopApplicationStatus.Cancelled && l.ToStatus == ShopApplicationStatus.Pending);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Đơn đăng ký không tồn tại (ApplicationId không tìm thấy) -> Ném NotFound 404 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_ResubmitShop_ApplicationNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_applicationId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopApplication?)null);

        // Act
        var act = () => _service.ResubmitAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID03", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop application was not found.");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Đơn đang ở trạng thái Pending (Chưa duyệt/từ chối) -> Ném Conflict 409 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_ResubmitShop_ApplicationStatusPending_ThrowsConflict_Abnormal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();
        var existingApp = CreateExistingApplication(ShopApplicationStatus.Pending, revisionNumber: 1);

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_applicationId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingApp);

        // Act
        var act = () => _service.ResubmitAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID04", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only rejected or cancelled shop applications can be edited and resubmitted.");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: Đơn đã ở trạng thái Approved (Đã được duyệt) -> Ném Conflict 409 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_ResubmitShop_ApplicationStatusApproved_ThrowsConflict_Abnormal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();
        var existingApp = CreateExistingApplication(ShopApplicationStatus.Approved, revisionNumber: 1);

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_applicationId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingApp);

        // Act
        var act = () => _service.ResubmitAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID05", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only rejected or cancelled shop applications can be edited and resubmitted.");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID06: Thiếu tài liệu bắt buộc BusinessLicense -> Ném BadRequest 400 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID06_ResubmitShop_MissingBusinessLicense_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();
        var existingApp = CreateExistingApplication(ShopApplicationStatus.Rejected, revisionNumber: 1);
        
        // Xóa BusinessLicense khỏi tài liệu cũ
        var docsToRemove = existingApp.Documents.Where(d => d.DocumentType == ShopDocumentType.BusinessLicense).ToList();
        foreach (var d in docsToRemove) existingApp.Documents.Remove(d);

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_applicationId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingApp);

        // Act
        var act = () => _service.ResubmitAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID06", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Business license document is required.");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID07: Thiếu tài liệu bắt buộc FoodSafetyCertificate -> Ném BadRequest 400 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID07_ResubmitShop_MissingFoodSafetyCertificate_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();
        var existingApp = CreateExistingApplication(ShopApplicationStatus.Rejected, revisionNumber: 1);
        
        // Xóa FoodSafetyCertificate khỏi tài liệu cũ
        var docsToRemove = existingApp.Documents.Where(d => d.DocumentType == ShopDocumentType.FoodSafetyCertificate).ToList();
        foreach (var d in docsToRemove) existingApp.Documents.Remove(d);

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_applicationId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingApp);

        // Act
        var act = () => _service.ResubmitAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID07", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Food safety certificate is required.");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID08: Tên cửa hàng rỗng -> Validator ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID08_ResubmitShop_EmptyName_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();
        request.Name = ""; // Tên rỗng

        // Act
        var act = () => _service.ResubmitAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID08", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Name");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID09: Giờ mở cửa >= Giờ đóng cửa -> Validator ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID09_ResubmitShop_OpeningTimeAfterClosingTime_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();
        request.OpeningTime = new TimeOnly(22, 0);
        request.ClosingTime = new TimeOnly(8, 0); // Opening >= Closing

        // Act
        var act = () => _service.ResubmitAsync(_userId, request, null, null, Array.Empty<IFormFile>(), Array.Empty<string>());

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID09", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "OpeningTime" || e.PropertyName == "ClosingTime");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID10: Số lượng file và document types không khớp -> Validator ném ValidationException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID10_ResubmitShop_DocumentCountMismatch_ThrowsValidationException_Abnormal()
    {
        // Arrange
        var request = CreateValidResubmitRequest();
        var files = new List<IFormFile>
        {
            CreateFakeFormFile("doc1.pdf", "application/pdf"),
            CreateFakeFormFile("doc2.pdf", "application/pdf")
        };
        var types = new List<string> { "BusinessLicense" }; // 2 files nhưng 1 type

        // Act
        var act = () => _service.ResubmitAsync(_userId, request, null, null, files, types);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<ValidationException>();
        PrintException("UTCID10", ex.Which.Message);
        ex.Which.Errors.Should().Contain(e => e.ErrorMessage.Contains("Each uploaded document must have a document type"));

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
