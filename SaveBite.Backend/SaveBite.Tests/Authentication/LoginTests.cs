using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaveBite.Backend.Caching;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.Authentication;

/// <summary>
/// Unit Tests for Feature: Login
/// Service: AuthService.LoginAsync & LoginRequestValidator
/// Function Code: UT-46
/// Sheet Name: UT_Login
/// </summary>
public class LoginTests
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
    private readonly LoginRequestValidator _validator = new();

    private const string ValidEmail = "user@savebite.vn";
    private const string UnregisteredEmail = "unregistered@savebite.vn";
    private const string UnverifiedEmail = "unverified@savebite.vn";
    private const string InvalidEmail = "invalid-email";
    private const string CorrectPassword = "SecurePass@123";
    private const string WrongPassword = "WrongPass@123";

    public LoginTests(ITestOutputHelper output)
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
    }

    private static User CreateValidUser(string email, bool isVerified = true)
    {
        var normalizedEmail = EmailAddressHelper.Normalize(email);
        return new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = "hashed-correct-password",
            FullName = "Nguyen Van A",
            EmailVerifiedAt = isVerified ? DateTime.UtcNow : null,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // TCID01: Email và mật khẩu đúng, tài khoản đã xác thực -> Trả về TokenPair (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_Login_ValidCredentials_ReturnsTokenPair_Normal()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = ValidEmail,
            Password = CorrectPassword
        };

        var normalizedEmail = EmailAddressHelper.Normalize(ValidEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var failureKey = RedisKeys.AuthLoginFailuresByEmail(subjectHash);
        var user = CreateValidUser(normalizedEmail, isVerified: true);

        var expectedTokens = new TokenPairResponse(
            "access-token-123",
            DateTime.UtcNow.AddMinutes(15),
            "refresh-token-456",
            DateTime.UtcNow.AddDays(30)
        );

        _redisMock.Setup(r => r.GetStringAsync(failureKey))
            .ReturnsAsync("0"); // 0 failed attempts < 5
        _authRepoMock.Setup(r => r.GetUserByEmailAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword(CorrectPassword, user.PasswordHash))
            .Returns(true);
        _refreshTokenServiceMock.Setup(r => r.IssueAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokens);
        _redisMock.Setup(r => r.RemoveAsync(failureKey))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token-123");
        result.RefreshToken.Should().Be("refresh-token-456");

        _redisMock.Verify(r => r.RemoveAsync(failureKey), Times.Once);
        _refreshTokenServiceMock.Verify(r => r.IssueAsync(user, It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine("TCID01 PASSED: Logged in successfully and returned valid token pair.");
    }

    // =========================================================================
    // TCID02: Email chưa đăng ký (user null) -> Ném 401 Unauthorized (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_Login_UnregisteredEmail_ThrowsUnauthorized_Abnormal()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = UnregisteredEmail,
            Password = CorrectPassword
        };

        var normalizedEmail = EmailAddressHelper.Normalize(UnregisteredEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var failureKey = RedisKeys.AuthLoginFailuresByEmail(subjectHash);

        _redisMock.Setup(r => r.GetStringAsync(failureKey))
            .ReturnsAsync("0");
        _authRepoMock.Setup(r => r.GetUserByEmailAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _redisMock.Setup(r => r.IncrementAsync(failureKey, It.IsAny<TimeSpan>()))
            .ReturnsAsync(1);

        // Act
        Func<Task> act = async () => await _service.LoginAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Which.Message.Should().Be("Email or password is incorrect.");

        _redisMock.Verify(r => r.IncrementAsync(failureKey, It.IsAny<TimeSpan>()), Times.Once);

        _output.WriteLine($"TCID02 PASSED: Threw expected AppException 401: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: Sai mật khẩu -> Ghi nhận thất bại, ném 401 Unauthorized (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_Login_WrongPassword_ThrowsUnauthorized_Abnormal()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = ValidEmail,
            Password = WrongPassword
        };

        var normalizedEmail = EmailAddressHelper.Normalize(ValidEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var failureKey = RedisKeys.AuthLoginFailuresByEmail(subjectHash);
        var user = CreateValidUser(normalizedEmail, isVerified: true);

        _redisMock.Setup(r => r.GetStringAsync(failureKey))
            .ReturnsAsync("1"); // 1 previous failure < 5
        _authRepoMock.Setup(r => r.GetUserByEmailAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword(WrongPassword, user.PasswordHash))
            .Returns(false);
        _redisMock.Setup(r => r.IncrementAsync(failureKey, It.IsAny<TimeSpan>()))
            .ReturnsAsync(2);

        // Act
        Func<Task> act = async () => await _service.LoginAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Which.Message.Should().Be("Email or password is incorrect.");

        _redisMock.Verify(r => r.IncrementAsync(failureKey, It.IsAny<TimeSpan>()), Times.Once);

        _output.WriteLine($"TCID03 PASSED: Threw expected AppException 401: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: Email chưa xác thực (EmailVerifiedAt null) -> Ném 401 Unauthorized (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_Login_UnverifiedEmail_ThrowsUnauthorized_Abnormal()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = UnverifiedEmail,
            Password = CorrectPassword
        };

        var normalizedEmail = EmailAddressHelper.Normalize(UnverifiedEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var failureKey = RedisKeys.AuthLoginFailuresByEmail(subjectHash);
        var user = CreateValidUser(normalizedEmail, isVerified: false); // Unverified

        _redisMock.Setup(r => r.GetStringAsync(failureKey))
            .ReturnsAsync("0");
        _authRepoMock.Setup(r => r.GetUserByEmailAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword(CorrectPassword, user.PasswordHash))
            .Returns(true);

        // Act
        Func<Task> act = async () => await _service.LoginAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Which.Message.Should().Be("Email address has not been verified.");

        _output.WriteLine($"TCID04 PASSED: Threw expected AppException 401: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID05: Thử đăng nhập sai quá số lần tối đa (>= 5) -> Ném 429 TooManyRequests (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_Login_FailureLimitExceeded_ThrowsTooManyRequests_Abnormal()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = ValidEmail,
            Password = CorrectPassword
        };

        var normalizedEmail = EmailAddressHelper.Normalize(ValidEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var failureKey = RedisKeys.AuthLoginFailuresByEmail(subjectHash);

        _redisMock.Setup(r => r.GetStringAsync(failureKey))
            .ReturnsAsync("5"); // Already reached 5 failed attempts
        _redisMock.Setup(r => r.GetTimeToLiveAsync(failureKey))
            .ReturnsAsync(TimeSpan.FromSeconds(300)); // 300 seconds left

        // Act
        Func<Task> act = async () => await _service.LoginAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        ex.Which.Message.Should().Contain("Too many failed login attempts.");

        _authRepoMock.Verify(r => r.GetUserByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"TCID05 PASSED: Threw expected AppException 429: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID06: Validator - Email sai định dạng -> Validation fails on Email (Abnormal - A)
    // =========================================================================
    [Fact]
    public void TCID06_Login_InvalidEmailFormat_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = InvalidEmail,
            Password = CorrectPassword
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Email));

        _output.WriteLine($"TCID06 PASSED: Validation failed on Email as expected: {result.Errors.First(e => e.PropertyName == "Email").ErrorMessage}");
    }

    // =========================================================================
    // TCID07: Validator - Password để trống -> Validation fails on Password (Boundary - B)
    // =========================================================================
    [Fact]
    public void TCID07_Login_EmptyPassword_ValidationFails_Boundary()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = ValidEmail,
            Password = string.Empty
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password));

        _output.WriteLine($"TCID07 PASSED: Validation failed on Password as expected: {result.Errors.First(e => e.PropertyName == "Password").ErrorMessage}");
    }
}
