using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
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
    private readonly Mock<IPlatformFeeConfigRepository> _repositoryMock = new();
    private readonly Mock<ILogger<PlatformFeeConfigService>> _loggerMock = new();
    private readonly PlatformFeeConfigService _service;

    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Guid _configId = Guid.NewGuid();

    public ViewFeeConfigurationTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformFeeConfigService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private PlatformFeeConfig CreateValidActiveConfig(decimal feeRate = 0.05m, DateTime? effectiveTo = null) => new()
    {
        Id = _configId,
        FeeRate = feeRate,
        EffectiveFrom = DateTime.UtcNow.AddDays(-10),
        EffectiveTo = effectiveTo,
        CreatedBy = _adminId,
        CreatedAt = DateTime.UtcNow.AddDays(-10)
    };

    // =========================================================================
    // TCID01: Active config exists -> Returns active config details (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetActiveConfig_ActiveConfigExists_ReturnsActiveConfigDetails_Normal()
    {
        // Arrange
        var existingConfig = CreateValidActiveConfig(feeRate: 0.05m);

        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);

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

        _repositoryMock.Verify(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: Active platform fee configuration retrieved successfully.");
    }

    // =========================================================================
    // TCID02: No active config in system -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetActiveConfig_NoActiveConfig_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeeConfig?)null);

        // Act
        Func<Task> act = async () => await _service.GetActiveConfigAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Active platform fee configuration not found.");

        _repositoryMock.Verify(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID02 PASSED: NotFound exception thrown as expected when active config is missing.");
    }

    // =========================================================================
    // TCID03: Active config with 0% fee rate -> Returns 0 fee percentage (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetActiveConfig_ZeroFeeRate_ReturnsZeroPercentage_Boundary()
    {
        // Arrange
        var zeroConfig = CreateValidActiveConfig(feeRate: 0.0m);

        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zeroConfig);

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
        var maxConfig = CreateValidActiveConfig(feeRate: 1.0m);

        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(maxConfig);

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
        var futureExpiration = DateTime.UtcNow.AddDays(30);
        var configWithEnd = CreateValidActiveConfig(feeRate: 0.08m, effectiveTo: futureExpiration);

        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configWithEnd);

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
    // TCID06: Cancellation token is propagated to repository (Normal - N)
    // =========================================================================
    // =========================================================================
    // TCID06: Active config with precision decimal fee rate -> Calculates accurate percentage (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID06_GetActiveConfig_PrecisionFeeRate_CalculatesAccuratePercentage_Normal()
    {
        // Arrange
        var precisionConfig = CreateValidActiveConfig(feeRate: 0.0275m);

        _repositoryMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(precisionConfig);

        // Act
        var result = await _service.GetActiveConfigAsync();

        // Assert
        result.Should().NotBeNull();
        result.FeeRate.Should().Be(0.0275m);
        result.FeePercentage.Should().Be(2.75m);

        _output.WriteLine("TCID06 PASSED: Precision decimal fee rate correctly converted to 2.75%.");
    }
}
