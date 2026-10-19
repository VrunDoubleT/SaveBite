using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Moq;
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
/// Unit Tests for Feature: View Shop Registration Status
/// Service: ShopApplicationService.GetMyApplicationAsync
/// Function Code: UT-23
/// Sheet Name: UT_ViewShopRegistrationStatus
/// </summary>
public class ViewShopRegistrationStatusTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopApplicationRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly ShopApplicationService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _applicationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ViewShopRegistrationStatusTests(ITestOutputHelper output)
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

    private ShopApplication CreateBaseApplication(ShopApplicationStatus status, int revisionNumber = 1)
    {
        var now = DateTime.UtcNow;
        return new ShopApplication
        {
            Id = _applicationId,
            ApplicantUserId = _userId,
            Name = "SaveBite Bistro",
            Description = "Delicious surplus bakery",
            BusinessLicenseNo = "BL-123456",
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
            PayosChecksumKey = "payos-checksum-key",
            Status = status,
            RevisionNumber = revisionNumber,
            LogoUrl = "https://res.cloudinary.com/savebite/logo.png",
            CoverImageUrl = "https://res.cloudinary.com/savebite/cover.jpg",
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now
        };
    }

    // =========================================================================
    // UTCID01: Đơn Pending, tài liệu IsCurrent = true -> Hiển thị trong response (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetMyApplication_PendingStatus_IsCurrentTrue_ReturnsDocument_Normal()
    {
        // Arrange
        var app = CreateBaseApplication(ShopApplicationStatus.Pending, revisionNumber: 1);
        
        app.Documents.Add(new ShopApplicationDocument
        {
            Id = Guid.NewGuid(),
            ApplicationId = _applicationId,
            DocumentType = ShopDocumentType.BusinessLicense,
            FileUrl = "https://res.cloudinary.com/savebite/new_license.pdf",
            OriginalFileName = "new_license.pdf",
            ContentType = "application/pdf",
            RevisionNumber = 1,
            IsCurrent = true,
            UploadedAt = DateTime.UtcNow.AddDays(-1)
        });

        app.ReviewLogs.Add(new ShopApplicationReviewLog
        {
            ApplicationId = _applicationId,
            FromStatus = null,
            ToStatus = ShopApplicationStatus.Pending,
            RevisionNumber = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        _repositoryMock
            .Setup(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var result = await _service.GetMyApplicationAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result!.Id.Should().Be(_applicationId);
        result.Status.Should().Be("Pending");
        result.Documents.Should().HaveCount(1);
        result.Documents.First().FileUrl.Should().Be("https://res.cloudinary.com/savebite/new_license.pdf");
        result.Documents.First().IsCurrent.Should().BeTrue();

        _repositoryMock.Verify(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Đơn Pending, tài liệu IsCurrent = false -> Bị loại bỏ, Documents rỗng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetMyApplication_PendingStatus_IsCurrentFalse_ExcludesDocument_Normal()
    {
        // Arrange
        var app = CreateBaseApplication(ShopApplicationStatus.Pending, revisionNumber: 1);
        
        app.Documents.Add(new ShopApplicationDocument
        {
            Id = Guid.NewGuid(),
            ApplicationId = _applicationId,
            DocumentType = ShopDocumentType.BusinessLicense,
            FileUrl = "https://res.cloudinary.com/savebite/old_license.pdf",
            OriginalFileName = "old_license.pdf",
            ContentType = "application/pdf",
            RevisionNumber = 1,
            IsCurrent = false, // Tài liệu cũ đã bị thay thế
            UploadedAt = DateTime.UtcNow.AddDays(-2)
        });

        _repositoryMock
            .Setup(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var result = await _service.GetMyApplicationAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result!.Id.Should().Be(_applicationId);
        result.Status.Should().Be("Pending");
        // Kiểm tra business rule: IsCurrent = false bị loại bỏ khỏi danh sách Documents
        result.Documents.Should().BeEmpty();

        _repositoryMock.Verify(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Đơn Rejected, tài liệu IsCurrent = true -> Hiển thị đơn kèm lý do từ chối của Admin (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetMyApplication_RejectedStatus_IncludesAdminRejectionReason_Normal()
    {
        // Arrange
        var app = CreateBaseApplication(ShopApplicationStatus.Rejected, revisionNumber: 1);
        
        app.Documents.Add(new ShopApplicationDocument
        {
            Id = Guid.NewGuid(),
            ApplicationId = _applicationId,
            DocumentType = ShopDocumentType.BusinessLicense,
            FileUrl = "https://res.cloudinary.com/savebite/license.pdf",
            OriginalFileName = "license.pdf",
            ContentType = "application/pdf",
            RevisionNumber = 1,
            IsCurrent = true,
            UploadedAt = DateTime.UtcNow.AddDays(-1)
        });

        app.ReviewLogs.Add(new ShopApplicationReviewLog
        {
            ApplicationId = _applicationId,
            FromStatus = ShopApplicationStatus.Pending,
            ToStatus = ShopApplicationStatus.Rejected,
            RevisionNumber = 1,
            Note = "Giấy phép kinh doanh đã hết hạn.",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });

        _repositoryMock
            .Setup(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var result = await _service.GetMyApplicationAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID03", result);
        result.Should().NotBeNull();
        result!.Status.Should().Be("Rejected");
        result.Documents.Should().HaveCount(1);
        result.ReviewLogs.Should().HaveCount(1);
        result.ReviewLogs.First().ToStatus.Should().Be("Rejected");
        result.ReviewLogs.First().Note.Should().Be("Giấy phép kinh doanh đã hết hạn.");

        _repositoryMock.Verify(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID04: Đơn Approved, tài liệu IsCurrent = true -> Hiển thị đơn đã phê duyệt (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetMyApplication_ApprovedStatus_ReturnsApprovedApplication_Normal()
    {
        // Arrange
        var app = CreateBaseApplication(ShopApplicationStatus.Approved, revisionNumber: 1);

        app.Documents.Add(new ShopApplicationDocument
        {
            Id = Guid.NewGuid(),
            ApplicationId = _applicationId,
            DocumentType = ShopDocumentType.BusinessLicense,
            FileUrl = "https://res.cloudinary.com/savebite/license.pdf",
            OriginalFileName = "license.pdf",
            ContentType = "application/pdf",
            RevisionNumber = 1,
            IsCurrent = true,
            UploadedAt = DateTime.UtcNow.AddDays(-1)
        });

        app.ReviewLogs.Add(new ShopApplicationReviewLog
        {
            ApplicationId = _applicationId,
            FromStatus = ShopApplicationStatus.Pending,
            ToStatus = ShopApplicationStatus.Approved,
            RevisionNumber = 1,
            Note = "Hồ sơ hợp lệ, đã duyệt mở quán.",
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        });

        _repositoryMock
            .Setup(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var result = await _service.GetMyApplicationAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID04", result);
        result.Should().NotBeNull();
        result!.Status.Should().Be("Approved");
        result.Documents.Should().HaveCount(1);
        result.ReviewLogs.First().ToStatus.Should().Be("Approved");

        _repositoryMock.Verify(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID05: Người dùng chưa từng gửi đơn đăng ký nào -> Trả về null (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetMyApplication_NoApplicationFound_ReturnsNull_Normal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopApplication?)null);

        // Act
        var result = await _service.GetMyApplicationAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID05", result);
        result.Should().BeNull();

        _repositoryMock.Verify(r => r.GetLatestForApplicantAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
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
