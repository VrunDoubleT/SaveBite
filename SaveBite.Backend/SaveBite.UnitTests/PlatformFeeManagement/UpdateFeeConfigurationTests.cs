using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
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
    private readonly Mock<IPlatformFeeConfigRepository> _repositoryMock = new();
    private readonly Mock<ILogger<PlatformFeeConfigService>> _loggerMock = new();
    private readonly PlatformFeeConfigService _service;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _currentConfigId = Guid.NewGuid();

    public UpdateFeeConfigurationTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformFeeConfigService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private PlatformFeeConfig CreateActiveConfig(decimal feeRate = 0.05m) => new()
    {
        Id = _currentConfigId,
        FeeRate = feeRate,
        EffectiveFrom = DateTime.UtcNow.AddDays(-30),
        EffectiveTo = null,
        CreatedBy = _adminId,
        CreatedAt = DateTime.UtcNow.AddDays(-30)
    };

    // =========================================================================
    // TCID01: Valid Rate (8.5%), Future Date (+1 day) -> Matches Expected Output (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_UpdateConfig_ValidRateAndFutureDate_MatchesExpectedOutput_Normal()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 8.5m,
            EffectiveFrom = futureDate
        };
        var currentConfig = CreateActiveConfig(feeRate: 0.05m);

        _repositoryMock
            .Setup(r => r.GetUpcomingConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeeConfig?)null);

        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentConfig);

        _repositoryMock
            .Setup(r => r.AddConfigAsync(It.IsAny<PlatformFeeConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.AddAuditLog(It.IsAny<AuditLog>()));

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateConfigAsync(_adminId, request);

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.085m);
        result.FeePercentage.Should().Be(8.5m);
        result.CreatedBy.Should().Be(_adminId);
        result.EffectiveFrom.Should().Be(futureDate);

        _repositoryMock.Verify(r => r.AddConfigAsync(
            It.Is<PlatformFeeConfig>(c => c.FeeRate == 0.085m && c.CreatedBy == _adminId),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.AddAuditLog(
            It.Is<AuditLog>(a => a.Action == "CreateScheduledFeeConfiguration" && a.ActorUserId == _adminId)),
            Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

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
            EffectiveFrom = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var act = () => _service.UpdateConfigAsync(_adminId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Platform fee rate must be between 0% and 100%");

        _repositoryMock.Verify(r => r.AddConfigAsync(It.IsAny<PlatformFeeConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

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
            EffectiveFrom = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var act = () => _service.UpdateConfigAsync(_adminId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Platform fee rate must be between 0% and 100%");

        _repositoryMock.Verify(r => r.AddConfigAsync(It.IsAny<PlatformFeeConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

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
            EffectiveFrom = DateTime.UtcNow.AddDays(-2)
        };

        // Act
        var act = () => _service.UpdateConfigAsync(_adminId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Effective date must be in the future or current time");

        _repositoryMock.Verify(r => r.AddConfigAsync(It.IsAny<PlatformFeeConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

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
            EffectiveFrom = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var act = () => _service.UpdateConfigAsync(Guid.Empty, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Contain("Only an Administrator may update platform-fee configuration");

        _repositoryMock.Verify(r => r.AddConfigAsync(It.IsAny<PlatformFeeConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID05 PASSED] Threw expected 403 Forbidden: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID06: Lower-Boundary Rate (0.0%), Immediate (null) -> Matches Expected Output (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID06_UpdateConfig_LowerBoundaryZeroRate_MatchesExpectedOutput_Boundary()
    {
        // Arrange
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 0.0m,
            EffectiveFrom = null
        };
        var currentConfig = CreateActiveConfig(feeRate: 0.05m);

        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentConfig);

        _repositoryMock
            .Setup(r => r.GetUpcomingConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeeConfig?)null);

        _repositoryMock
            .Setup(r => r.AddConfigAsync(It.IsAny<PlatformFeeConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.AddAuditLog(It.IsAny<AuditLog>()));

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateConfigAsync(_adminId, request);

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.0m);
        result.FeePercentage.Should().Be(0.0m);
        result.CreatedBy.Should().Be(_adminId);

        _repositoryMock.Verify(r => r.AddConfigAsync(
            It.Is<PlatformFeeConfig>(c => c.FeeRate == 0.0m),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID06 PASSED] Lower-boundary FeeRate: {result.FeePercentage}%, EffectiveFrom: {result.EffectiveFrom}");
    }

    // =========================================================================
    // TCID07: Upper-Boundary Rate (100.0%), Future Date (+7 days) -> Matches Expected Output (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID07_UpdateConfig_UpperBoundary100Rate_MatchesExpectedOutput_Boundary()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(7);
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 100.0m,
            EffectiveFrom = futureDate
        };
        var currentConfig = CreateActiveConfig(feeRate: 0.05m);

        _repositoryMock
            .Setup(r => r.GetUpcomingConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeeConfig?)null);

        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentConfig);

        _repositoryMock
            .Setup(r => r.AddConfigAsync(It.IsAny<PlatformFeeConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.AddAuditLog(It.IsAny<AuditLog>()));

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateConfigAsync(_adminId, request);

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(1.0m);
        result.FeePercentage.Should().Be(100.0m);
        result.CreatedBy.Should().Be(_adminId);
        result.EffectiveFrom.Should().Be(futureDate);

        _repositoryMock.Verify(r => r.AddConfigAsync(
            It.Is<PlatformFeeConfig>(c => c.FeeRate == 1.0m),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID07 PASSED] Upper-boundary FeeRate: {result.FeePercentage}%, EffectiveFrom: {result.EffectiveFrom}");
    }

    // =========================================================================
    // TCID08: Valid Rate (7.5%), Immediate (null) -> Matches Expected Output (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID08_UpdateConfig_ImmediateUpdate_ClosesActiveConfig_MatchesExpectedOutput_Normal()
    {
        // Arrange
        var request = new UpdatePlatformFeeConfigRequest
        {
            FeeRate = 7.5m,
            EffectiveFrom = null
        };
        var currentConfig = CreateActiveConfig(feeRate: 0.05m);

        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentConfig);

        _repositoryMock
            .Setup(r => r.GetUpcomingConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeeConfig?)null);

        _repositoryMock
            .Setup(r => r.AddConfigAsync(It.IsAny<PlatformFeeConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.AddAuditLog(It.IsAny<AuditLog>()));

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateConfigAsync(_adminId, request);

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.075m);
        result.FeePercentage.Should().Be(7.5m);
        result.CreatedBy.Should().Be(_adminId);

        // Verify that existing active config had its EffectiveTo closed
        currentConfig.EffectiveTo.Should().NotBeNull();

        _repositoryMock.Verify(r => r.AddConfigAsync(
            It.Is<PlatformFeeConfig>(c => c.FeeRate == 0.075m),
            It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.AddAuditLog(
            It.Is<AuditLog>(a => a.Action == "UpdateFeeConfiguration" && a.ActorUserId == _adminId)),
            Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID08 PASSED] Immediate FeeRate: {result.FeePercentage}%, Closed Previous Config EffectiveTo: {currentConfig.EffectiveTo}");
    }
}
