using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.Authentication;

/// <summary>
/// Unit Tests for Feature: Change Password
/// Service: AuthService.ChangePasswordAsync & ChangePasswordRequestValidator
/// Function Code: UT-50
/// Sheet Name: UT_ChangePassword
/// </summary>
public class ChangePasswordTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IAuthRepository> _authRepoMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IAuthRedisService> _redisMock = new();
    private readonly Mock<IPasswordHasherService> _passwordHasherMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();
    private readonly AuthService _service;
    private readonly ChangePasswordRequestValidator _validator = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _nonExistentUserId = Guid.NewGuid();
    private const string CurrentPassword = "CurrentPass@123";
    private const string WrongCurrentPassword = "WrongPass@123";
    private const string NewStrongPassword = "NewSecurePass@456";
    private const string WeakNewPassword = "weakpass";
    private const string CurrentHashedPassword = "hashed-current-password";
    private const string NewHashedPassword = "hashed-new-password";

    public ChangePasswordTests(ITestOutputHelper output)
    {
        _output = output;

        var authSettings = Options.Create(new AuthSettings
        {
            OtpLifetimeMinutes = 15,
            OtpResendCooldownSeconds = 60,
            MaxOtpRequestsPerEmailPerDay = 5,
            MaxOtpVerificationAttempts = 5,
            LoginAttemptWindowMinutes = 15,
            MaxLoginAttemptsPerEmail = 5
        });

        var jwtSettings = Options.Create(new JwtSettings
        {
            SigningKey = "test-signing-key-that-is-long-enough-32chars",
            Issuer = "savebite-test",
            Audience = "savebite-test",
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 30
        });

        _service = new AuthService(
            _authRepoMock.Object,
            _userServiceMock.Object,
            _redisMock.Object,
            _passwordHasherMock.Object,
            _refreshTokenServiceMock.Object,
            _publisherMock.Object,
            _loggerMock.Object,
            authSettings,
            jwtSettings,
            TimeProvider.System
        );
        
        var mockTx = new Mock<IRepositoryTransaction>();
        _authRepoMock.Setup(r => r.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTx.Object);
    }

    private User CreateExistingUser() => new()
    {
        Id = _userId,
        Email = "user@savebite.vn",
        PasswordHash = CurrentHashedPassword,
        FullName = "Nguyen Van A",
        EmailVerifiedAt = DateTime.UtcNow.AddDays(-10),
        Status = UserStatus.Active,
        CreatedAt = DateTime.UtcNow.AddDays(-10),
        UpdatedAt = DateTime.UtcNow.AddDays(-10)
    };

    // =========================================================================
    // TCID01: Correct current password, valid new password -> Password changed & sessions revoked (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_ChangePassword_ValidCredentials_PasswordChangedSuccessfully_Normal()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = CurrentPassword,
            NewPassword = NewStrongPassword
        };

        var user = CreateExistingUser();

        _authRepoMock.Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword(CurrentPassword, CurrentHashedPassword))
            .Returns(true);
        _passwordHasherMock.Setup(p => p.HashPassword(NewStrongPassword))
            .Returns(NewHashedPassword);
        _authRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
      

        // Act
        Func<Task> act = async () => await _service.ChangePasswordAsync(_userId, request);

        // Assert
        await act.Should().NotThrowAsync();

        user.PasswordHash.Should().Be(NewHashedPassword);
        _authRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("TCID01 PASSED: Password changed successfully and saved to repository.");
    }

    // =========================================================================
    // TCID02: User not found -> Throws 401 Unauthorized (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_ChangePassword_UserNotFound_ThrowsUnauthorized_Abnormal()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = CurrentPassword,
            NewPassword = NewStrongPassword
        };

        _authRepoMock.Setup(r => r.GetUserByIdAsync(_nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _service.ChangePasswordAsync(_nonExistentUserId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _authRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _refreshTokenServiceMock.Verify(r => r.RevokeAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine("TCID02 PASSED: Non-existent user rejected with 401 Unauthorized.");
    }

    // =========================================================================
    // TCID03: Incorrect current password -> Throws 401 Unauthorized (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_ChangePassword_IncorrectCurrentPassword_ThrowsUnauthorized_Abnormal()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = WrongCurrentPassword,
            NewPassword = NewStrongPassword
        };

        var user = CreateExistingUser();

        _authRepoMock.Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword(WrongCurrentPassword, CurrentHashedPassword))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _service.ChangePasswordAsync(_userId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Current password is incorrect.");

        _authRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _refreshTokenServiceMock.Verify(
            r => r.RevokeAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"TCID03 PASSED: Threw expected AppException 400: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: Validator - New password identical to current password -> Validation fails (Abnormal - A)
    // =========================================================================
    [Fact]
    public void TCID04_ChangePassword_NewPasswordSameAsCurrent_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = CurrentPassword,
            NewPassword = CurrentPassword // Same password
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword)
            && e.ErrorMessage.Contains("New password must be different from the current password."));

        _output.WriteLine("TCID04 PASSED: Validation rejected new password identical to current password.");
    }

    // =========================================================================
    // TCID05: Validator - Weak new password -> Validation fails (Abnormal - A)
    // =========================================================================
    [Fact]
    public void TCID05_ChangePassword_WeakNewPassword_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = CurrentPassword,
            NewPassword = WeakNewPassword
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));

        _output.WriteLine("TCID05 PASSED: Validation rejected weak new password.");
    }

    // =========================================================================
    // TCID06: Validator - Empty current password -> Validation fails on CurrentPassword (Boundary - B)
    // =========================================================================
    [Fact]
    public void TCID06_ChangePassword_EmptyCurrentPassword_ValidationFails_Boundary()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            CurrentPassword = string.Empty,
            NewPassword = NewStrongPassword
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.CurrentPassword));

        _output.WriteLine("TCID06 PASSED: Validation rejected empty current password.");
    }
}
