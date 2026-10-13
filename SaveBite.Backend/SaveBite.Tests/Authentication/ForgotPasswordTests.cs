using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaveBite.Backend.Caching;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.Authentication;

/// <summary>
/// Unit Tests for Feature: Forgot Password
/// Service: AuthService.RequestPasswordResetAsync & ForgotPasswordRequestValidator
/// Function Code: UT-47
/// Sheet Name: UT_ForgotPassword
/// </summary>
public class ForgotPasswordTests
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
    private readonly ForgotPasswordRequestValidator _validator = new();

    private const string RegisteredEmail = "user@savebite.vn";
    private const string UnregisteredEmail = "unregistered@savebite.vn";
    private const string InvalidEmail = "invalid-email";
    private const string StrongNewPassword = "NewSecurePass@123";
    private const string WeakNewPassword = "weakpass";

    public ForgotPasswordTests(ITestOutputHelper output)
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

        _passwordHasherMock
            .Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns("hashed-new-password");

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
    }

    // =========================================================================
    // TCID01: Registered email, rate allowed -> Sends OTP email successfully (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_ForgotPassword_RegisteredEmail_OtpPublishedSuccessfully_Normal()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = RegisteredEmail,
            NewPassword = StrongNewPassword
        };

        var normalizedEmail = EmailAddressHelper.Normalize(RegisteredEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var pendingKey = RedisKeys.AuthPendingPasswordReset(subjectHash);

        _redisMock.Setup(r => r.AcquireOtpRequestPermitAsync(
            "password-reset",
            subjectHash,
            It.IsAny<TimeSpan>(),
            It.IsAny<int>(),
            It.IsAny<TimeSpan>()))
            .ReturnsAsync(new RateLimitDecision(true, null));

        _authRepoMock.Setup(r => r.EmailExistsAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _redisMock.Setup(r => r.SetJsonAsync(pendingKey, It.IsAny<PendingPasswordReset>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _redisMock.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _redisMock.Setup(r => r.RemoveAsync(It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);
        _publisherMock.Setup(p => p.PublishAsync(
            RabbitMqRoutingKeys.EmailRoutingKey,
            It.IsAny<EmailNotificationEvent>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Func<Task> act = async () => await _service.RequestPasswordResetAsync(request);

        // Assert
        await act.Should().NotThrowAsync();

        _publisherMock.Verify(p => p.PublishAsync(
            RabbitMqRoutingKeys.EmailRoutingKey,
            It.Is<EmailNotificationEvent>(e => e.To == normalizedEmail && e.Subject == "Reset your SaveBite password"),
            It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine("TCID01 PASSED: Password reset requested and OTP published successfully.");
    }

    // =========================================================================
    // TCID02: Unregistered email -> Silently returns to prevent user enumeration (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_ForgotPassword_UnregisteredEmail_ReturnsSilentlyWithoutEmail_Normal()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = UnregisteredEmail,
            NewPassword = StrongNewPassword
        };

        var normalizedEmail = EmailAddressHelper.Normalize(UnregisteredEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);

        _redisMock.Setup(r => r.AcquireOtpRequestPermitAsync(
            "password-reset",
            subjectHash,
            It.IsAny<TimeSpan>(),
            It.IsAny<int>(),
            It.IsAny<TimeSpan>()))
            .ReturnsAsync(new RateLimitDecision(true, null));

        _authRepoMock.Setup(r => r.EmailExistsAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Unknown email

        // Act
        Func<Task> act = async () => await _service.RequestPasswordResetAsync(request);

        // Assert
        await act.Should().NotThrowAsync();

        _publisherMock.Verify(p => p.PublishAsync(
            It.IsAny<string>(),
            It.IsAny<EmailNotificationEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine("TCID02 PASSED: Unknown email safely returned without leaking account existence.");
    }

    // =========================================================================
    // TCID03: Rate limit exceeded -> Throws 429 TooManyRequests (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_ForgotPassword_RateLimitExceeded_ThrowsTooManyRequests_Abnormal()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = RegisteredEmail,
            NewPassword = StrongNewPassword
        };

        var normalizedEmail = EmailAddressHelper.Normalize(RegisteredEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);

        _redisMock.Setup(r => r.AcquireOtpRequestPermitAsync(
            "password-reset",
            subjectHash,
            It.IsAny<TimeSpan>(),
            It.IsAny<int>(),
            It.IsAny<TimeSpan>()))
            .ReturnsAsync(new RateLimitDecision(false, TimeSpan.FromSeconds(45)));

        // Act
        Func<Task> act = async () => await _service.RequestPasswordResetAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        ex.Which.Message.Should().Contain("Too many OTP requests. Try again in 45 seconds.");

        _authRepoMock.Verify(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"TCID03 PASSED: Threw expected AppException 429: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: Validator - Email format invalid -> Validation fails on Email (Abnormal - A)
    // =========================================================================
    [Fact]
    public void TCID04_ForgotPassword_InvalidEmailFormat_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = InvalidEmail,
            NewPassword = StrongNewPassword
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ForgotPasswordRequest.Email));

        _output.WriteLine($"TCID04 PASSED: Validation failed on Email as expected: {result.Errors.First(e => e.PropertyName == "Email").ErrorMessage}");
    }

    // =========================================================================
    // TCID05: Validator - Weak new password -> Validation fails on NewPassword (Abnormal - A)
    // =========================================================================
    [Fact]
    public void TCID05_ForgotPassword_WeakNewPassword_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = RegisteredEmail,
            NewPassword = WeakNewPassword
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ForgotPasswordRequest.NewPassword));

        _output.WriteLine($"TCID05 PASSED: Validation failed on NewPassword as expected: {result.Errors.First(e => e.PropertyName == "NewPassword").ErrorMessage}");
    }
}
