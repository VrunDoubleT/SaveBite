using SaveBite.Backend.Data;
using SaveBite.Backend.Services.Interfaces;
using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.PlatformFeeManagement;

/// <summary>
/// Unit Tests for Feature: Update Fee Configuration
/// Service: PlatformFeeConfigService.UpdateConfigAsync
/// Function Code: UT-103
/// Sheet Name: UT_UpdateFeeConfiguration
/// </summary>
public class UpdateFeeConfigurationTests
{
    private readonly ITestOutputHelper _output;
    private readonly AppDbContext _db;
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly Mock<INotificationQueue> _notificationMock = new();
    private readonly PlatformFeeConfigService _service;

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _currentConfigId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public UpdateFeeConfigurationTests(ITestOutputHelper output)
    {
        _output = output;
        _db = TestDbContextFactory.Create();
        _service = new PlatformFeeConfigService(_db, TimeProvider.System, _auditMock.Object, _notificationMock.Object);
    }

    private static DateTime UtcSecondsOffset(double days)
    {
        var dt = DateTime.UtcNow.AddDays(days);
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc);
    }

    private async Task<PlatformFeeConfig> SeedActiveConfigAsync(decimal feeRate = 0.05m)
    {
        if (!_db.Users.Any(u => u.Id == _adminId))
        {
            _db.Users.Add(new User
            {
                Id = _adminId,
                Email = "admin@savebite.test",
                PasswordHash = "hash",
                FullName = "System Admin",
                Role = UserRole.Admin,
                Status = UserStatus.Active
            });
        }

        var config = new PlatformFeeConfig
        {
            Id = _currentConfigId,
            FeeRate = feeRate,
            EffectiveFrom = UtcSecondsOffset(-30),
            EffectiveTo = null,
            LifecycleStatus = "Published",
            Reason = "Initial platform fee rate",
            ActivationProcessedAt = UtcSecondsOffset(-30),
            CreatedBy = _adminId,
            CreatedAt = UtcSecondsOffset(-30)
        };

        _db.PlatformFeeConfigs.Add(config);
        await _db.SaveChangesAsync();
        return config;
    }

    // =========================================================================
    // TCID01: Valid Rate (8.5%), Future Date (+1 day) -> Matches Expected Output (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_UpdateConfig_ValidRateAndFutureDate_MatchesExpectedOutput_Normal()
    {
        // Arrange
        await SeedActiveConfigAsync(feeRate: 0.05m);
        var futureDate = UtcSecondsOffset(1);
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 8.5m,
            EffectiveFrom = futureDate,
            Reason = "Quarterly platform commission update"
        };

        // Act
        var result = await _service.UpdateConfigAsync(_adminId, request);

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.085m);
        result.FeePercentage.Should().Be(8.5m);
        result.CreatedBy.Should().Be(_adminId);
        result.EffectiveFrom.Should().Be(futureDate);
        result.Status.Should().Be("Scheduled");

        var saved = await _db.PlatformFeeConfigs.AsNoTracking().SingleOrDefaultAsync(c => c.Id == result.Id);
        saved.Should().NotBeNull();
        saved!.FeeRate.Should().Be(0.085m);
        saved.CreatedBy.Should().Be(_adminId);

        _auditMock.Verify(r => r.Record(
            It.Is<AuditLog>(a => a.Action == "FEE_SCHEDULE_CREATED" && a.ActorUserId == _adminId)),
            Times.Once);

        _output.WriteLine($"[TCID01 PASSED] FeeRate configured: {result.FeePercentage}%, EffectiveFrom: {result.EffectiveFrom}");
    }

    // =========================================================================
    // TCID02: Negative Rate (-1.0%) -> Throws 400 Bad Request (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_UpdateConfig_NegativeRate_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = -1.0m,
            EffectiveFrom = UtcSecondsOffset(1),
            Reason = "Invalid negative rate test"
        };

        // Act
        var act = () => _service.UpdateConfigAsync(_adminId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Platform fee rate must be between 0% and 100%");

        (await _db.PlatformFeeConfigs.CountAsync()).Should().Be(0);
        _auditMock.Verify(r => r.Record(It.IsAny<AuditLog>()), Times.Never);

        _output.WriteLine($"[TCID02 PASSED] Threw expected 400 Bad Request: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: Exceeds 100% (105.0%) -> Throws 400 Bad Request (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_UpdateConfig_Exceeds100Percent_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 105.0m,
            EffectiveFrom = UtcSecondsOffset(1),
            Reason = "Invalid over-100 rate test"
        };

        // Act
        var act = () => _service.UpdateConfigAsync(_adminId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Platform fee rate must be between 0% and 100%");

        (await _db.PlatformFeeConfigs.CountAsync()).Should().Be(0);
        _auditMock.Verify(r => r.Record(It.IsAny<AuditLog>()), Times.Never);

        _output.WriteLine($"[TCID03 PASSED] Threw expected 400 Bad Request: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: Valid Rate (6.0%), Past Date (-2 days) -> Throws 400 Bad Request (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_UpdateConfig_PastDate_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 6.0m,
            EffectiveFrom = UtcSecondsOffset(-2),
            Reason = "Backdated schedule should fail"
        };

        // Act
        var act = () => _service.UpdateConfigAsync(_adminId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Effective date cannot be in the past");

        (await _db.PlatformFeeConfigs.CountAsync()).Should().Be(0);
        _auditMock.Verify(r => r.Record(It.IsAny<AuditLog>()), Times.Never);

        _output.WriteLine($"[TCID04 PASSED] Threw expected 400 Bad Request: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID05: Unauthorized Non-Admin (Empty Admin ID) -> Throws 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_UpdateConfig_UnauthorizedAdmin_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 8.5m,
            EffectiveFrom = UtcSecondsOffset(1),
            Reason = "Unauthorized actor attempt"
        };

        // Act
        var act = () => _service.UpdateConfigAsync(Guid.Empty, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _db.PlatformFeeConfigs.CountAsync()).Should().Be(0);
        _auditMock.Verify(r => r.Record(It.IsAny<AuditLog>()), Times.Never);

        _output.WriteLine($"[TCID05 PASSED] Threw expected 403 Forbidden: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID06: Lower-Boundary Rate (0.0%), Immediate (null) -> Matches Expected Output (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID06_UpdateConfig_LowerBoundaryZeroRate_MatchesExpectedOutput_Boundary()
    {
        // Arrange
        await SeedActiveConfigAsync(feeRate: 0.05m);
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 0.0m,
            EffectiveFrom = null,
            Reason = "Promotional zero platform fee"
        };

        // Act
        var result = await _service.UpdateConfigAsync(_adminId, request);

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.0m);
        result.FeePercentage.Should().Be(0.0m);
        result.CreatedBy.Should().Be(_adminId);

        var saved = await _db.PlatformFeeConfigs.AsNoTracking().SingleAsync(c => c.Id == result.Id);
        saved.FeeRate.Should().Be(0.0m);

        _output.WriteLine($"[TCID06 PASSED] Lower-boundary FeeRate: {result.FeePercentage}%, EffectiveFrom: {result.EffectiveFrom}");
    }

    // =========================================================================
    // TCID07: Upper-Boundary Rate (100.0%), Future Date (+7 days) -> Matches Expected Output (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID07_UpdateConfig_UpperBoundary100Rate_MatchesExpectedOutput_Boundary()
    {
        // Arrange
        await SeedActiveConfigAsync(feeRate: 0.05m);
        var futureDate = UtcSecondsOffset(7);
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 100.0m,
            EffectiveFrom = futureDate,
            Reason = "Maximum boundary fee test"
        };

        // Act
        var result = await _service.UpdateConfigAsync(_adminId, request);

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(1.0m);
        result.FeePercentage.Should().Be(100.0m);
        result.CreatedBy.Should().Be(_adminId);
        result.EffectiveFrom.Should().Be(futureDate);

        var saved = await _db.PlatformFeeConfigs.AsNoTracking().SingleAsync(c => c.Id == result.Id);
        saved.FeeRate.Should().Be(1.0m);

        _output.WriteLine($"[TCID07 PASSED] Upper-boundary FeeRate: {result.FeePercentage}%, EffectiveFrom: {result.EffectiveFrom}");
    }

    // =========================================================================
    // TCID08: Valid Rate (7.5%), Immediate (null) -> Closes Active Config & Creates New (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID08_UpdateConfig_ImmediateUpdate_ClosesActiveConfig_MatchesExpectedOutput_Normal()
    {
        // Arrange
        var currentConfig = await SeedActiveConfigAsync(feeRate: 0.05m);
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 7.5m,
            EffectiveFrom = null,
            Reason = "Immediate fee adjustment to 7.5%"
        };

        // Act
        var result = await _service.UpdateConfigAsync(_adminId, request);

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.075m);
        result.FeePercentage.Should().Be(7.5m);
        result.CreatedBy.Should().Be(_adminId);

        var reloadedPrevious = await _db.PlatformFeeConfigs.AsNoTracking().SingleAsync(c => c.Id == currentConfig.Id);
        reloadedPrevious.EffectiveTo.Should().NotBeNull();
        reloadedPrevious.EffectiveTo.Should().Be(result.EffectiveFrom);

        _auditMock.Verify(r => r.Record(
            It.Is<AuditLog>(a => a.Action == "FEE_SCHEDULE_CREATED" && a.ActorUserId == _adminId)),
            Times.Once);

        _output.WriteLine($"[TCID08 PASSED] Immediate FeeRate: {result.FeePercentage}%, Closed Previous Config EffectiveTo: {reloadedPrevious.EffectiveTo}");
    }

    // =========================================================================
    // TCID09: Missing or Short Reason -> Throws 400 Bad Request (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID09_UpdateConfig_ShortReason_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 8.0m,
            EffectiveFrom = UtcSecondsOffset(1),
            Reason = "no"
        };

        // Act
        var act = () => _service.UpdateConfigAsync(_adminId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Provide a reason between 3 and 500 characters");

        _output.WriteLine($"[TCID09 PASSED] Threw expected 400 Bad Request: {ex.Which.Message}");
    }
}
