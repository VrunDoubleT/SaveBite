using SaveBite.Backend.Data;
using SaveBite.Backend.Services.Interfaces;
using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.PlatformFeeManagement;

/// <summary>
/// Unit Tests for Feature: View Fee Configuration
/// Service: PlatformFeeConfigService.GetActiveConfigAsync
/// Function Code: UT-102
/// Sheet Name: UT_ViewFeeConfiguration
/// </summary>
public class ViewFeeConfigurationTests
{
    private readonly ITestOutputHelper _output;
    private readonly AppDbContext _db;
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly Mock<INotificationQueue> _notificationMock = new();
    private readonly PlatformFeeConfigService _service;

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _configId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ViewFeeConfigurationTests(ITestOutputHelper output)
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

    private async Task<PlatformFeeConfig> SeedConfigAsync(decimal feeRate = 0.05m, DateTime? effectiveTo = null)
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
            Id = _configId,
            FeeRate = feeRate,
            EffectiveFrom = UtcSecondsOffset(-10),
            EffectiveTo = effectiveTo,
            LifecycleStatus = "Published",
            Reason = "Initial configuration",
            CreatedBy = _adminId,
            CreatedAt = UtcSecondsOffset(-10)
        };

        _db.PlatformFeeConfigs.Add(config);
        await _db.SaveChangesAsync();
        return config;
    }

    // =========================================================================
    // TCID01: Active config exists -> Returns active config details (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetActiveConfig_ActiveConfigExists_ReturnsActiveConfigDetails_Normal()
    {
        // Arrange
        var existingConfig = await SeedConfigAsync(feeRate: 0.05m);

        // Act
        var result = await _service.GetActiveConfigAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingConfig.Id);
        result.FeeRate.Should().Be(0.05m);
        result.FeePercentage.Should().Be(5.0m);
        result.EffectiveFrom.Should().Be(existingConfig.EffectiveFrom);
        result.EffectiveTo.Should().BeNull();
        result.CreatedBy.Should().Be(_adminId);
        result.Status.Should().Be("Active");

        _output.WriteLine("TCID01 PASSED: Active platform fee configuration retrieved successfully.");
    }

    // =========================================================================
    // TCID02: No active config in system -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetActiveConfig_NoActiveConfig_ThrowsNotFound_Abnormal()
    {
        // Arrange - empty database (no active config seeded)

        // Act
        Func<Task> act = async () => await _service.GetActiveConfigAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Active platform fee configuration not found.");

        _output.WriteLine("TCID02 PASSED: NotFound exception thrown as expected when active config is missing.");
    }

    // =========================================================================
    // TCID03: Active config with 0% fee rate -> Returns 0 fee percentage (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetActiveConfig_ZeroFeeRate_ReturnsZeroPercentage_Boundary()
    {
        // Arrange
        await SeedConfigAsync(feeRate: 0.0m);

        // Act
        var result = await _service.GetActiveConfigAsync();

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.0m);
        result.FeePercentage.Should().Be(0.0m);

        _output.WriteLine("TCID03 PASSED: Zero fee rate correctly returns 0% fee percentage.");
    }

    // =========================================================================
    // TCID04: Active config with 100% fee rate -> Returns 100 fee percentage (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID04_GetActiveConfig_MaxFeeRate_ReturnsHundredPercentage_Boundary()
    {
        // Arrange
        await SeedConfigAsync(feeRate: 1.0m);

        // Act
        var result = await _service.GetActiveConfigAsync();

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(1.0m);
        result.FeePercentage.Should().Be(100.0m);

        _output.WriteLine("TCID04 PASSED: 100% fee rate correctly returns 100% fee percentage.");
    }

    // =========================================================================
    // TCID05: Active config with future expiration window -> Returns EffectiveTo date (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID05_GetActiveConfig_WithEffectiveToDate_ReturnsExpirationDate_Normal()
    {
        // Arrange
        var futureExpiration = UtcSecondsOffset(30);
        await SeedConfigAsync(feeRate: 0.08m, effectiveTo: futureExpiration);

        // Act
        var result = await _service.GetActiveConfigAsync();

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.08m);
        result.FeePercentage.Should().Be(8.0m);
        result.EffectiveTo.Should().Be(futureExpiration);

        _output.WriteLine("TCID05 PASSED: Configuration with scheduled expiration returned correctly.");
    }

    // =========================================================================
    // TCID06: Active config with precision decimal fee rate -> Calculates accurate percentage (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID06_GetActiveConfig_PrecisionFeeRate_CalculatesAccuratePercentage_Normal()
    {
        // Arrange
        await SeedConfigAsync(feeRate: 0.0275m);

        // Act
        var result = await _service.GetActiveConfigAsync();

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.0275m);
        result.FeePercentage.Should().Be(2.75m);

        _output.WriteLine("TCID06 PASSED: Precision decimal fee rate correctly converted to 2.75%.");
    }
}
