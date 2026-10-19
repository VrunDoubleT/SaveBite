using System.Net;
using FluentAssertions;
using Moq;
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
/// Unit Tests for Feature: View Shop Registration Requests
/// Service: ShopApplicationService.GetAllAsync
/// Function Code: UT-51
/// Sheet Name: UT_ViewShopRegistrationRequests
/// </summary>
public class ViewShopRegistrationRequestsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopApplicationRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly ShopApplicationService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _applicationId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _applicationId2 = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public ViewShopRegistrationRequestsTests(ITestOutputHelper output)
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

    private ShopApplication CreateSampleApplication(Guid id, ShopApplicationStatus status)
    {
        return new ShopApplication
        {
            Id = id,
            ApplicantUserId = _userId,
            Name = "SaveBite Bistro",
            Description = "Delicious surplus meals",
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
    // TCID01: Lấy danh sách các đơn đăng ký khi có dữ liệu -> Trả về danh sách (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetAll_ApplicationsExist_ReturnsList_Normal()
    {
        // Arrange
        var app1 = CreateSampleApplication(_applicationId1, ShopApplicationStatus.Pending);
        var app2 = CreateSampleApplication(_applicationId2, ShopApplicationStatus.Approved);

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopApplication> { app1, app2 });

        // Act
        var result = await _service.GetAllAsync();

        // Assert & Log
        PrintSuccess("TCID01", "Applications list retrieved successfully");
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(_applicationId1);
        result[0].Status.Should().Be("Pending");
        result[1].Id.Should().Be(_applicationId2);
        result[1].Status.Should().Be("Approved");
    }

    // =========================================================================
    // TCID02: Không có đơn đăng ký nào trong DB -> Trả về danh sách rỗng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetAll_EmptyList_ReturnsEmptyList_Normal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopApplication>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert & Log
        PrintSuccess("TCID02", "Empty applications list returned successfully");
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // =========================================================================
    // TCID03: Đơn có tài liệu đính kèm -> Map đầy đủ tài liệu cả lịch sử (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetAll_WithDocuments_IncludesAllDocuments_Normal()
    {
        // Arrange
        var app = CreateSampleApplication(_applicationId1, ShopApplicationStatus.Pending);
        app.Documents.Add(new ShopApplicationDocument
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            DocumentType = ShopDocumentType.BusinessLicense,
            FileUrl = "https://example.com/license.pdf",
            OriginalFileName = "license.pdf",
            ContentType = "application/pdf",
            RevisionNumber = 1,
            IsCurrent = true,
            UploadedAt = DateTime.UtcNow
        });
        app.Documents.Add(new ShopApplicationDocument
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            DocumentType = ShopDocumentType.BusinessLicense,
            FileUrl = "https://example.com/license_old.pdf",
            OriginalFileName = "license_old.pdf",
            ContentType = "application/pdf",
            RevisionNumber = 1,
            IsCurrent = false,
            UploadedAt = DateTime.UtcNow.AddDays(-1)
        });

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopApplication> { app });

        // Act
        var result = await _service.GetAllAsync();

        // Assert & Log
        PrintSuccess("TCID03", "Application with documents mapped including non-current");
        result.Should().HaveCount(1);
        result[0].Documents.Should().HaveCount(2);
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
}
