using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.Authentication;

/// <summary>
/// Unit Tests for Feature: Logout
/// Service: RefreshTokenService.RevokeAsync & LogoutRequestValidator
/// Function Code: UT-49
/// Sheet Name: UT_Logout
/// </summary>
public class LogoutTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IAuthRepository> _authRepoMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly RefreshTokenService _service;
    private readonly LogoutRequestValidator _validator = new();

    private const string ActiveToken = "valid-active-token-123";
    private const string RevokedToken = "already-revoked-token-456";
    private const string NonExistentToken = "non-existent-token-789";

    public LogoutTests(ITestOutputHelper output)
    {
        _output = output;

        var jwtSettings = Options.Create(new JwtSettings
        {
            SigningKey = "test-signing-key-that-is-long-enough-32chars",
            Issuer = "savebite-test",
            Audience = "savebite-test",
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 30
        });

        _service = new RefreshTokenService(
            _authRepoMock.Object,
            _jwtServiceMock.Object,
            jwtSettings,
            TimeProvider.System
        );
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    // =========================================================================
    // TCID01: Active token -> Revoked successfully, SaveChanges called (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_Logout_ActiveToken_RevokedSuccessfully_Normal()
    {
        // Arrange
        var tokenHash = HashToken(ActiveToken);
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null,
            RevokeReason = null
        };

        _authRepoMock.Setup(r => r.GetRefreshTokenWithUserAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);
        _authRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RevokeAsync(ActiveToken, "User logout");

        // Assert
        existingToken.RevokedAt.Should().NotBeNull();
        existingToken.RevokeReason.Should().Be("User logout");

        _authRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine("TCID01 PASSED: Active refresh token revoked and changes saved.");
    }

    // =========================================================================
    // TCID02: Already revoked token -> Idempotent no-op, SaveChanges not called (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_Logout_AlreadyRevokedToken_NoOp_Normal()
    {
        // Arrange
        var tokenHash = HashToken(RevokedToken);
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow.AddHours(-1), // Already revoked
            RevokeReason = "Previous logout"
        };

        _authRepoMock.Setup(r => r.GetRefreshTokenWithUserAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingToken);

        // Act
        await _service.RevokeAsync(RevokedToken, "User logout");

        // Assert
        existingToken.RevokeReason.Should().Be("Previous logout"); // Unchanged

        _authRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine("TCID02 PASSED: Already revoked token safely ignored without modifying state.");
    }

    // =========================================================================
    // TCID03: Non-existent token -> Idempotent no-op, SaveChanges not called (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID03_Logout_NonExistentToken_NoOp_Normal()
    {
        // Arrange
        var tokenHash = HashToken(NonExistentToken);

        _authRepoMock.Setup(r => r.GetRefreshTokenWithUserAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        await _service.RevokeAsync(NonExistentToken, "User logout");

        // Assert
        _authRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine("TCID03 PASSED: Non-existent token safely ignored without exception.");
    }

    // =========================================================================
    // TCID04: Validator - Empty RefreshToken -> Validation fails on RefreshToken (Boundary - B)
    // =========================================================================
    [Fact]
    public void TCID04_Logout_EmptyRefreshToken_ValidationFails_Boundary()
    {
        // Arrange
        var request = new LogoutRequest
        {
            RefreshToken = string.Empty
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LogoutRequest.RefreshToken));

        _output.WriteLine($"TCID04 PASSED: Validation failed on empty RefreshToken: {result.Errors.First(e => e.PropertyName == "RefreshToken").ErrorMessage}");
    }

    // =========================================================================
    // TCID05: Validator - RefreshToken exceeds max length (512) -> Validation fails (Boundary - B)
    // =========================================================================
    [Fact]
    public void TCID05_Logout_RefreshTokenExceedsMaxLength_ValidationFails_Boundary()
    {
        // Arrange
        var request = new LogoutRequest
        {
            RefreshToken = new string('a', 513) // 513 chars > max 512
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LogoutRequest.RefreshToken));

        _output.WriteLine($"TCID05 PASSED: Validation failed on RefreshToken exceeding 512 characters.");
    }
}
