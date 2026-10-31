using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.AuditLogManagement;

/// <summary>
/// Unit Tests for Feature: View Audit Log Details
/// Service: AuditLogService.GetAuditLogByIdAsync
/// Function Code: UT-128
/// Sheet Name: UT_ViewAuditLogDetails
/// </summary>
public class ViewAuditLogDetailsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IAuditLogRepository> _repositoryMock = new();
    private readonly AuditLogService _service;

    public static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidLogId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid NonExistentLogId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public ViewAuditLogDetailsTests(ITestOutputHelper output)
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

    private Backend.Models.Entities.AuditLog CreateSampleLog() => new()
    {
        Id = ValidLogId,
        ActorUserId = AdminUserId,
        ActorUser = CreateAdminUser(),
        ActorType = AuditActorType.User,
        Action = "UpdateFeeConfiguration",
        TargetType = "PlatformFeeConfig",
        TargetId = Guid.NewGuid(),
        OldValuesJson = "{\"feeRate\":0.05,\"secretToken\":\"sec_123\"}",
        NewValuesJson = "{\"feeRate\":0.08,\"secretToken\":\"sec_456\"}",
        Reason = "Increase rate for Q4",
        CorrelationId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow
    };

    // =========================================================================
    // TCID01: Existing Log ID -> Returns Sanitized Details (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetAuditLogById_ExistingId_ReturnsSanitizedDetails_Normal()
    {
        // Arrange
        var log = CreateSampleLog();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(ValidLogId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

        // Act
        var result = await _service.GetAuditLogByIdAsync(ValidLogId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidLogId);
        result.Action.Should().Be("UpdateFeeConfiguration");
        result.ActorName.Should().Be("Super Admin");
        result.Outcome.Should().Be("Succeeded");
        result.OldValues.Should().NotBeNull();
        result.OldValues!["secretToken"].Should().Be(AuditPayloadSanitizer.Redacted);
        result.NewValues!["secretToken"].Should().Be(AuditPayloadSanitizer.Redacted);

        _repositoryMock.Verify(r => r.GetByIdAsync(ValidLogId, It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"[TCID01 PASSED] 200 OK - Retrieved audit log {ValidLogId} successfully with secrets redacted.");
    }

    // =========================================================================
    // TCID02: Non-Existent Log ID -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetAuditLogById_NonExistentId_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(NonExistentLogId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Backend.Models.Entities.AuditLog?)null);

        // Act
        var act = () => _service.GetAuditLogByIdAsync(NonExistentLogId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Contain("Audit log entry not found");

        _output.WriteLine($"[TCID02 PASSED] 404 NotFound thrown as expected: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: System Actor Type -> Displays 'System' as Actor Name (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetAuditLogById_SystemActor_ReturnsSystemAsActorName_Normal()
    {
        // Arrange
        var systemLog = CreateSampleLog();
        systemLog.ActorType = AuditActorType.System;
        systemLog.ActorUserId = null;
        systemLog.ActorUser = null;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(ValidLogId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemLog);

        // Act
        var result = await _service.GetAuditLogByIdAsync(ValidLogId);

        // Assert
        result.ActorName.Should().Be("System");
        result.ActorEmail.Should().BeNull();

        _output.WriteLine("[TCID03 PASSED] System actor properly mapped to 'System' name.");
    }
}
