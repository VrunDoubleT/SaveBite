using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StoreRegistration;

/// <summary>
/// Unit Tests for Feature: View Shop Registration Details
/// Service: ShopApplicationService.GetByIdAsync
/// Function Code: UT-52
/// Sheet Name: UT_ViewShopRegistrationDetails
/// </summary>
public class ViewShopRegistrationDetailsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopApplicationRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly ShopApplicationService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _existingAppId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _nonExistentAppId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public ViewShopRegistrationDetailsTests(ITestOutputHelper output)
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

    private ShopApplication CreateSampleApplication(ShopApplicationStatus status)
    {
        return new ShopApplication
        {
            Id = _existingAppId,
            ApplicantUserId = _userId,
            Name = "SaveBite Bistro",
            Description = "Delicious surplus meals",
            BusinessLicenseNo = "BL-999888",
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
    // TCID01: Đơn tồn tại -> Trả về thông tin chi tiết đầy đủ (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetById_ExistingApplication_ReturnsDetails_Normal()
    {
        // Arrange
        var app = CreateSampleApplication(ShopApplicationStatus.Pending);
        _repositoryMock
            .Setup(r => r.GetByIdAsync(_existingAppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var result = await _service.GetByIdAsync(_existingAppId);

        // Assert & Log
        PrintSuccess("TCID01", "Shop application details retrieved successfully");
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingAppId);
        result.Name.Should().Be("SaveBite Bistro");
        result.Status.Should().Be("Pending");
        result.BusinessLicenseNo.Should().Be("BL-999888");
        result.HasPayosConfiguration.Should().BeTrue();
    }

    // =========================================================================
    // TCID02: Đơn không tồn tại trong DB -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetById_NonExistentApplication_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(_nonExistentAppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopApplication?)null);

        // Act
        var act = () => _service.GetByIdAsync(_nonExistentAppId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop application was not found.");
    }

    // =========================================================================
    // TCID03: Đơn có lịch sử review và tài liệu -> Trả về chi tiết đầy đủ (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetById_WithReviewLogs_ReturnsDetailsWithLogs_Normal()
    {
        // Arrange
        var app = CreateSampleApplication(ShopApplicationStatus.Approved);
        app.ReviewLogs.Add(new ShopApplicationReviewLog
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            AdminId = Guid.NewGuid(),
            FromStatus = ShopApplicationStatus.Pending,
            ToStatus = ShopApplicationStatus.Approved,
            RevisionNumber = 1,
            Note = "Verified successfully",
            CreatedAt = DateTime.UtcNow
        });

        _repositoryMock
            .Setup(r => r.GetByIdAsync(_existingAppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var result = await _service.GetByIdAsync(_existingAppId);

        // Assert & Log
        PrintSuccess("TCID03", "Application details with review logs retrieved");
        result.ReviewLogs.Should().HaveCount(1);
        result.ReviewLogs[0].ToStatus.Should().Be("Approved");
        result.ReviewLogs[0].Note.Should().Be("Verified successfully");
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
