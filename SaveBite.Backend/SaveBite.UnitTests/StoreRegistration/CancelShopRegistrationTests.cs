using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
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
/// Unit Tests for Feature: Cancel Shop Registration
/// Service: ShopApplicationService.CancelAsync
/// Function Code: UT-25
/// Sheet Name: UT_CancelShopRegistration
/// </summary>
public class CancelShopRegistrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopApplicationRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly ShopApplicationService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _existingAppId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _nonExistentAppId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public CancelShopRegistrationTests(ITestOutputHelper output)
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

    private ShopApplication CreateApplication(ShopApplicationStatus status, int revisionNumber = 1)
    {
        var now = DateTime.UtcNow;
        return new ShopApplication
        {
            Id = _existingAppId,
            ApplicantUserId = _userId,
            Name = "SaveBite Bistro",
            AddressLine = "123 Le Loi Street",
            Latitude = 10.7769,
            Longitude = 106.7009,
            BankName = "Vietcombank",
            BankAccountNumber = "987654321",
            BankAccountHolder = "NGUYEN VAN A",
            PayosClientId = "payos-client-id",
            PayosApiKey = "payos-api-key",
            PayosChecksumKey = "payos-checksum-key",
            Status = status,
            RevisionNumber = revisionNumber,
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-1)
        };
    }

    // =========================================================================
    // UTCID01: Hủy đơn Pending thành công -> Status: Cancelled, thêm ReviewLog (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_CancelApplication_PendingStatus_ShouldSucceed_Normal()
    {
        // Arrange
        var app = CreateApplication(ShopApplicationStatus.Pending, revisionNumber: 1);

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_existingAppId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        await _service.CancelAsync(_userId, _existingAppId);

        // Assert & Log
        PrintSuccess("UTCID01", new { app.Id, Status = app.Status.ToString(), app.RevisionNumber });
        app.Status.Should().Be(ShopApplicationStatus.Cancelled);
        app.ReviewLogs.Should().ContainSingle(l =>
            l.FromStatus == ShopApplicationStatus.Pending &&
            l.ToStatus == ShopApplicationStatus.Cancelled &&
            l.RevisionNumber == 1);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Không tìm thấy đơn đăng ký (Non-existent ID) -> Ném NotFound 404 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID02_CancelApplication_NonExistentId_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_nonExistentAppId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopApplication?)null);

        // Act
        var act = () => _service.CancelAsync(_userId, _nonExistentAppId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID02", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop application was not found.");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: Đơn đã ở trạng thái Approved -> Ném Conflict 409 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_CancelApplication_StatusApproved_ThrowsConflict_Abnormal()
    {
        // Arrange
        var app = CreateApplication(ShopApplicationStatus.Approved, revisionNumber: 1);

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_existingAppId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var act = () => _service.CancelAsync(_userId, _existingAppId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID03", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only a pending shop application can be cancelled.");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Đơn đã ở trạng thái Rejected -> Ném Conflict 409 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_CancelApplication_StatusRejected_ThrowsConflict_Abnormal()
    {
        // Arrange
        var app = CreateApplication(ShopApplicationStatus.Rejected, revisionNumber: 1);

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_existingAppId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var act = () => _service.CancelAsync(_userId, _existingAppId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID04", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only a pending shop application can be cancelled.");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: Đơn đã ở trạng thái Cancelled -> Ném Conflict 409 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_CancelApplication_StatusAlreadyCancelled_ThrowsConflict_Abnormal()
    {
        // Arrange
        var app = CreateApplication(ShopApplicationStatus.Cancelled, revisionNumber: 1);

        _repositoryMock
            .Setup(r => r.GetByIdForApplicantAsync(_existingAppId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(app);

        // Act
        var act = () => _service.CancelAsync(_userId, _existingAppId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID05", ex.Which.Message);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only a pending shop application can be cancelled.");

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
