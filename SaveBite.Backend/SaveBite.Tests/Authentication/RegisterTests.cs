using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.Authentication;

/// <summary>
/// Unit Tests for Feature: Register
/// Service: AuthService.RequestRegistrationAsync & RegisterRequestValidator
/// Function Code: UT-44
/// Sheet Name: UT_Register
/// </summary>
public class RegisterTests
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
    private readonly RegisterRequestValidator _validator = new();

    private const string NewEmail = "newuser@savebite.vn";
    private const string ExistingEmail = "existing@savebite.vn";
    private const string InvalidEmail = "invalid-email";
    private const string StrongPassword = "SecurePass@123";
    private const string WeakPassword = "abc123";
    private const string ValidFullName = "Nguyen Van A";
    private const string TooShortFullName = "N";

    public RegisterTests(ITestOutputHelper output)
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
            .Returns("hashed-password");

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
    // TCID01: Email chưa tồn tại, OTP request được phép -> Gửi OTP thành công (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_Register_NewEmail_OtpSentSuccessfully_Normal()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = NewEmail,
            Password = StrongPassword,
            FullName = ValidFullName
        };

        _authRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _redisMock
            .Setup(r => r.AcquireOtpRequestPermitAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(new RateLimitDecision(true, null));

        _redisMock
            .Setup(r => r.SetJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _redisMock
            .Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _redisMock
            .Setup(r => r.RemoveAsync(It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);

        _publisherMock
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var act = () => _service.RequestRegistrationAsync(request);

        // Assert & Log
        await act.Should().NotThrowAsync();
        PrintSuccess("TCID01", "OTP registration email sent successfully");

        _authRepoMock.Verify(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Email đã tồn tại trong DB -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_Register_EmailAlreadyExists_ThrowsConflict_Abnormal()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = ExistingEmail,
            Password = StrongPassword,
            FullName = ValidFullName
        };

        _authRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.RequestRegistrationAsync(request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("An account with this email already exists.");

        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID03: Vượt quá giới hạn OTP request -> Ném 429 TooManyRequests (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_Register_RateLimitExceeded_ThrowsTooManyRequests_Abnormal()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = NewEmail,
            Password = StrongPassword,
            FullName = ValidFullName
        };

        _authRepoMock
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _redisMock
            .Setup(r => r.AcquireOtpRequestPermitAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(new RateLimitDecision(false, TimeSpan.FromSeconds(60)));

        // Act
        var act = () => _service.RequestRegistrationAsync(request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        ex.Which.Message.Should().Contain("Too many OTP requests.");

        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID04: Email sai format "invalid-email" -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_Register_InvalidEmailFormat_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = InvalidEmail,
            Password = StrongPassword,
            FullName = ValidFullName
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("TCID04", result.Errors.First().ErrorMessage);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    // =========================================================================
    // TCID05: Password yếu "abc123" (thiếu chữ hoa, ký tự đặc biệt) -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_Register_WeakPassword_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = NewEmail,
            Password = WeakPassword,
            FullName = ValidFullName
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("TCID05", result.Errors.First().ErrorMessage);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    // =========================================================================
    // TCID06: FullName chỉ 1 ký tự "N" (dưới min 2) -> Validator từ chối (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID06_Register_TooShortFullName_ValidationFails_Boundary()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = NewEmail,
            Password = StrongPassword,
            FullName = TooShortFullName
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("TCID06", result.Errors.First().ErrorMessage);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    // =========================================================
    // Helper Methods for Formatting Log Output
    // =========================================================
    private void PrintSuccess(string tcid, string message, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }

    private void PrintException(string tcid, AppException ex)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"ERROR CODE: {ex.ErrorCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }

    private void PrintValidationError(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 BadRequest (Validation Failed)");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }
}
