using System.Net;
using FluentAssertions;
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
/// Unit Tests for Feature: Approve Shop Registration
/// Service: ShopApplicationService.ReviewAsync (Decision: Approved)
/// Function Code: UT-53
/// Sheet Name: UT_ApproveShopRegistration
/// </summary>
public class ApproveShopRegistrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopApplicationRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly ShopApplicationService _service;

    private readonly Guid _adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _applicantUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _existingAppId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _nonExistentAppId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public ApproveShopRegistrationTests(ITestOutputHelper output)
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

    private ShopApplication CreateSampleApplication(ShopApplicationStatus status, User? applicantUser = null, Shop? shop = null)
    {
        var user = applicantUser ?? new User
        {
            Id = _applicantUserId,
            Email = "applicant@savebite.vn",
            FullName = "Store Applicant",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active
        };

        return new ShopApplication
        {
            Id = _existingAppId,
            ApplicantUserId = _applicantUserId,
            ApplicantUser = user,
            Shop = shop,
            Name = "SaveBite Bistro",
            Description = "Delicious surplus bakery",
            BusinessLicenseNo = "BL-123456",
            AddressLine = "123 Le Loi Street",
            Ward = "Ben Nghe",
            District = "District 1",
            City = "Ho Chi Minh City",
            Latitude = 10.7769,
            Longitude = 106.7009,
            BankName = "Vietcombank",
            BankAccountNumber = "987654321",
            BankAccountHolder = "NGUYEN VAN A",
            PayosClientId = "payos-client-id",
            PayosApiKey = "payos-api-key",
            PayosChecksumKey = "payos-checksum-key",
            Status = status,
            RevisionNumber = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    // =========================================================================
    // TCID01: Đơn ở trạng thái Pending, thông tin hợp lệ -> Duyệt thành công (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_Approve_PendingApplication_EligibleApplicant_ApprovesSuccessfully_Normal()
    {
        // Arrange
        var app = CreateSampleApplication(ShopApplicationStatus.Pending);
        var request = new ReviewShopApplicationRequest
        {
            Decision = "Approved",
            Note = "All business requirements verified."
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(_existingAppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var result = await _service.ReviewAsync(_adminId, _existingAppId, request);

        // Assert & Log
        PrintSuccess("TCID01", "Shop application approved successfully");
        result.Should().NotBeNull();
        result.Status.Should().Be("Approved");
        app.Status.Should().Be(ShopApplicationStatus.Approved);
        app.ReviewLogs.Should().HaveCount(1);
        app.ReviewLogs.First().AdminId.Should().Be(_adminId);
        app.ReviewLogs.First().ToStatus.Should().Be(ShopApplicationStatus.Approved);

        _repositoryMock.Verify(r => r.SaveApprovedApplicationAsync(app, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Đơn không tồn tại trong DB -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_Approve_ApplicationNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new ReviewShopApplicationRequest
        {
            Decision = "Approved"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(_nonExistentAppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopApplication?)null);

        // Act
        var act = () => _service.ReviewAsync(_adminId, _nonExistentAppId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop application was not found.");

        _repositoryMock.Verify(r => r.SaveApprovedApplicationAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID03: Đơn không ở trạng thái Pending (đã Approved hoặc Rejected) -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_Approve_ApplicationNotPending_ThrowsConflict_Abnormal()
    {
        // Arrange
        var app = CreateSampleApplication(ShopApplicationStatus.Approved);
        var request = new ReviewShopApplicationRequest
        {
            Decision = "Approved"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(_existingAppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var act = () => _service.ReviewAsync(_adminId, _existingAppId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only pending applications can be approved or rejected.");

        _repositoryMock.Verify(r => r.SaveApprovedApplicationAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID04: Đã tồn tại Shop cho đơn này -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_Approve_ShopAlreadyCreated_ThrowsConflict_Abnormal()
    {
        // Arrange
        var existingShop = new Shop { Id = Guid.NewGuid(), Name = "Existing Shop" };
        var app = CreateSampleApplication(ShopApplicationStatus.Pending, shop: existingShop);
        var request = new ReviewShopApplicationRequest
        {
            Decision = "Approved"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(_existingAppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var act = () => _service.ReviewAsync(_adminId, _existingAppId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("A shop has already been created for this application.");

        _repositoryMock.Verify(r => r.SaveApprovedApplicationAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID05: Người đăng ký là tài khoản Admin không hợp lệ -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_Approve_ApplicantRoleIneligible_ThrowsConflict_Abnormal()
    {
        // Arrange
        var adminUser = new User
        {
            Id = _applicantUserId,
            Email = "admin@savebite.vn",
            FullName = "Admin Account",
            Role = UserRole.Admin,
            Status = UserStatus.Active
        };
        var app = CreateSampleApplication(ShopApplicationStatus.Pending, applicantUser: adminUser);
        var request = new ReviewShopApplicationRequest
        {
            Decision = "Approved"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(_existingAppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var act = () => _service.ReviewAsync(_adminId, _existingAppId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only an eligible user account can become a store owner.");

        _repositoryMock.Verify(r => r.SaveApprovedApplicationAsync(It.IsAny<ShopApplication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================
    // Helper Methods
    // =========================================================
    private void PrintSuccess(string tcid, string message, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }

    private void PrintException(string tcid, AppException ex)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"ERROR CODE: {ex.ErrorCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }
}
