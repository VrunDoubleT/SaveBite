using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.AuditLogManagement;

/// <summary>
/// Unit Tests for Feature: View Audit Log
/// Service: AuditLogService.GetAuditLogsAsync
/// Function Code: UT-127
/// Sheet Name: UT_ViewAuditLog
/// </summary>
public class ViewAuditLogTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IAuditLogRepository> _repositoryMock = new();
    private readonly AuditLogService _service;

    public static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid TargetEntityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AuditLogId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid AuditLogId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public ViewAuditLogTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new AuditLogService(_repositoryMock.Object);
    }

    private User CreateAdminUser() => new()
    {
        Id = AdminUserId,
        FullName = "Super Admin",
        Email = "admin@savebite.com",
        Role = UserRole.Admin,
        Status = UserStatus.Active
    };

    private Backend.Models.Entities.AuditLog CreateAuditLog(
        Guid id,
        string action,
        string targetType,
        string? oldVal = null,
        string? newVal = null) => new()
    {
        Id = id,
        ActorUserId = AdminUserId,
        ActorUser = CreateAdminUser(),
        ActorType = AuditActorType.User,
        Action = action,
        TargetType = targetType,
        TargetId = TargetEntityId,
        OldValuesJson = oldVal,
        NewValuesJson = newVal,
        Reason = "Routine compliance update",
        CorrelationId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow
    };

    // =========================================================================
    // TCID01: Valid Request, Returns Paged Audit Logs & Filter Options (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetAuditLogs_ValidRequest_ReturnsPagedLogsAndFilters_Normal()
    {
        // Arrange
        var request = new GetAuditLogsRequest(Page: 1, PageSize: 10);
        var logs = new List<Backend.Models.Entities.AuditLog>
        {
            CreateAuditLog(AuditLogId1, "SuspendAccount", "User", "{\"status\":\"Active\"}", "{\"status\":\"Suspended\"}"),
            CreateAuditLog(AuditLogId2, "UpdateFeeConfig", "PlatformFeeConfig", "{\"rate\":0.05}", "{\"rate\":0.08}")
        };
        var pagedLogs = PagedResult<Backend.Models.Entities.AuditLog>.Create(logs, 1, 10, 2);
        var filterOptions = (new List<string> { "SuspendAccount", "UpdateFeeConfig" }, new List<string> { "PlatformFeeConfig", "User" });

        _repositoryMock
            .Setup(r => r.GetPagedLogsAsync(request, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedLogs);

        _repositoryMock
            .Setup(r => r.GetDistinctFiltersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(filterOptions);

        // Act
        var result = await _service.GetAuditLogsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Page.Items.Should().HaveCount(2);
        result.Page.TotalItems.Should().Be(2);
        result.Page.Items[0].Id.Should().Be(AuditLogId1);
        result.Page.Items[0].Action.Should().Be("SuspendAccount");
        result.Page.Items[0].Outcome.Should().Be("Succeeded");
        result.Page.Items[0].ActorName.Should().Be("Super Admin");
        result.FilterOptions.Actions.Should().Contain("SuspendAccount");
        result.FilterOptions.TargetTypes.Should().Contain("User");

        _repositoryMock.Verify(r => r.GetPagedLogsAsync(request, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.GetDistinctFiltersAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Retrieved {result.Page.Items.Count} audit logs successfully.");
    }

    // =========================================================================
    // TCID02: Invalid Date Range (FromDate > ToDate) -> Throws 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetAuditLogs_FromDateLaterThanToDate_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new GetAuditLogsRequest(
            FromDate: new DateOnly(2026, 10, 30),
            ToDate: new DateOnly(2026, 10, 15)
        );

        // Act
        var act = () => _service.GetAuditLogsAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("FromDate cannot be later than ToDate");

        _repositoryMock.Verify(r => r.GetPagedLogsAsync(It.IsAny<GetAuditLogsRequest>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID02 PASSED] 400 BadRequest thrown as expected: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: Sensitive Data Sanitization Check (BR-163 Secrets Redaction) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetAuditLogs_PayloadContainsSecretKeys_RedactsSensitiveValues_Normal()
    {
        // Arrange
        var request = new GetAuditLogsRequest();
        var rawOld = "{\"password\":\"plainSecret123\",\"apiKey\":\"pk_live_abcdef\",\"name\":\"Shop A\"}";
        var rawNew = "{\"password\":\"newPassword456\",\"apiKey\":\"pk_live_xyz890\",\"name\":\"Shop A Renamed\"}";

        var logs = new List<Backend.Models.Entities.AuditLog>
        {
            CreateAuditLog(AuditLogId1, "UpdateCredentials", "Shop", rawOld, rawNew)
        };
        var pagedLogs = PagedResult<Backend.Models.Entities.AuditLog>.Create(logs, 1, 10, 1);

        _repositoryMock
            .Setup(r => r.GetPagedLogsAsync(request, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedLogs);
        _repositoryMock
            .Setup(r => r.GetDistinctFiltersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<string> { "UpdateCredentials" }, new List<string> { "Shop" }));

        // Act
        var result = await _service.GetAuditLogsAsync(request);

        // Assert
        result.Should().NotBeNull();
        var item = result.Page.Items[0];
        item.OldValues.Should().NotBeNull();
        item.NewValues.Should().NotBeNull();

        item.OldValues!["password"].Should().Be(AuditPayloadSanitizer.Redacted);
        item.OldValues["apiKey"].Should().Be(AuditPayloadSanitizer.Redacted);
        item.OldValues["name"].Should().Be("Shop A");

        item.NewValues!["password"].Should().Be(AuditPayloadSanitizer.Redacted);
        item.NewValues["apiKey"].Should().Be(AuditPayloadSanitizer.Redacted);
        item.NewValues["name"].Should().Be("Shop A Renamed");

        _output.WriteLine("[TCID03 PASSED] BR-163 Compliant: Sensitive passwords and apiKeys redacted into [REDACTED].");
    }

    // =========================================================================
    // TCID04: Empty Result Boundary -> Returns Empty PagedResult (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID04_GetAuditLogs_NoLogsMatchFilter_ReturnsEmptyList_Boundary()
    {
        // Arrange
        var request = new GetAuditLogsRequest(Search: "NonExistentKeyword");
        var pagedLogs = PagedResult<Backend.Models.Entities.AuditLog>.Create(new List<Backend.Models.Entities.AuditLog>(), 1, 10, 0);

        _repositoryMock
            .Setup(r => r.GetPagedLogsAsync(request, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedLogs);
        _repositoryMock
            .Setup(r => r.GetDistinctFiltersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<string>(), new List<string>()));

        // Act
        var result = await _service.GetAuditLogsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Page.Items.Should().BeEmpty();
        result.Page.TotalItems.Should().Be(0);

        _output.WriteLine("[TCID04 PASSED] 200 OK - Empty audit log list returned properly.");
    }
}
