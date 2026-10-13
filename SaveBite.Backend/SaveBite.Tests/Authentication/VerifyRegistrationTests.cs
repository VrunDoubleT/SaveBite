using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using SaveBite.Backend.Models.DTOs;
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
/// Unit Tests for Feature: Verify Registration
/// Service: AuthService.VerifyRegistrationAsync & VerifyRegistrationRequestValidator
/// Function Code: UT-45
/// Sheet Name: UT_VerifyRegistration
/// </summary>
public class VerifyRegistrationTests
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
    private readonly VerifyRegistrationRequestValidator _validator = new();

    private const string SigningKey = "test-signing-key-that-is-long-enough-32chars";
    private const string ValidEmail = "newuser@savebite.vn";
    private const string ExistingEmail = "existing@savebite.vn";
    private const string ExpiredEmail = "expired@savebite.vn";
    private const string InvalidEmail = "invalid-email";
    private const string ValidOtp = "123456";
    private const string IncorrectOtp = "999999";
    private const string ShortOtp = "12345";
    private const string Salt = "test-salt-123";

    public VerifyRegistrationTests(ITestOutputHelper output)
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
            SigningKey = SigningKey,
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

    private static string CreateOtpRecordJson(string purpose, string email, string otp, string salt)
    {
        var normalizedEmail = EmailAddressHelper.Normalize(email);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SigningKey));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{purpose}:{normalizedEmail}:{salt}:{otp}")));
        return JsonSerializer.Serialize(new OtpRecord(salt, hash));
    }

    // =========================================================================
    // TCID01: OTP hợp lệ, pending hợp lệ -> Verify thành công & tạo User (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_VerifyRegistration_ValidOtp_UserCreatedSuccessfully_Normal()
    {
        // Arrange
        var request = new VerifyRegistrationRequest
        {
            Email = ValidEmail,
            Otp = ValidOtp
        };

        var normalizedEmail = EmailAddressHelper.Normalize(ValidEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var pendingKey = RedisKeys.AuthPendingRegistration(subjectHash);
        var otpKey = RedisKeys.AuthOtp("registration", subjectHash);
        var attemptsKey = RedisKeys.AuthOtpAttempts("registration", subjectHash);

        var pending = new PendingRegistration(normalizedEmail, "hashed-pw", "Nguyen Van A");
        var rawOtpRecord = CreateOtpRecordJson("registration", normalizedEmail, ValidOtp, Salt);

        _redisMock.Setup(r => r.GetJsonAsync<PendingRegistration>(pendingKey))
            .ReturnsAsync(pending);
        _redisMock.Setup(r => r.GetStringAsync(otpKey))
            .ReturnsAsync(rawOtpRecord);
        _redisMock.Setup(r => r.DeleteIfValueMatchesAsync(otpKey, rawOtpRecord))
            .ReturnsAsync(true);
        _redisMock.Setup(r => r.RemoveAsync(attemptsKey))
            .Returns(Task.CompletedTask);
        _redisMock.Setup(r => r.RemoveAsync(pendingKey))
            .Returns(Task.CompletedTask);

        _authRepoMock.Setup(r => r.EmailExistsAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _authRepoMock.Setup(r => r.TryAddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _service.VerifyRegistrationAsync(request);

        // Assert
        await act.Should().NotThrowAsync();

        _authRepoMock.Verify(r => r.TryAddUserAsync(
            It.Is<User>(u => u.Email == normalizedEmail && u.FullName == "Nguyen Van A"),
            It.IsAny<CancellationToken>()), Times.Once);
        _redisMock.Verify(r => r.RemoveAsync(pendingKey), Times.Once);

        _output.WriteLine("TCID01 PASSED: User registration verified and account created successfully.");
    }

    // =========================================================================
    // TCID02: Pending data không tồn tại hoặc hết hạn -> Ném 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_VerifyRegistration_PendingDataNotFoundOrExpired_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new VerifyRegistrationRequest
        {
            Email = ExpiredEmail,
            Otp = ValidOtp
        };

        var normalizedEmail = EmailAddressHelper.Normalize(ExpiredEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var pendingKey = RedisKeys.AuthPendingRegistration(subjectHash);

        _redisMock.Setup(r => r.GetJsonAsync<PendingRegistration>(pendingKey))
            .ReturnsAsync((PendingRegistration?)null);

        // Act
        Func<Task> act = async () => await _service.VerifyRegistrationAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Registration data or OTP has expired.");
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvalidOtp);

        _output.WriteLine($"TCID02 PASSED: Threw expected AppException 400: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: OTP record không tồn tại hoặc hết hạn trong Redis -> Ném 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_VerifyRegistration_OtpRecordNotFoundOrExpired_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new VerifyRegistrationRequest
        {
            Email = ValidEmail,
            Otp = ValidOtp
        };

        var normalizedEmail = EmailAddressHelper.Normalize(ValidEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var pendingKey = RedisKeys.AuthPendingRegistration(subjectHash);
        var otpKey = RedisKeys.AuthOtp("registration", subjectHash);

        var pending = new PendingRegistration(normalizedEmail, "hashed-pw", "Nguyen Van A");

        _redisMock.Setup(r => r.GetJsonAsync<PendingRegistration>(pendingKey))
            .ReturnsAsync(pending);
        _redisMock.Setup(r => r.GetStringAsync(otpKey))
            .ReturnsAsync((string?)null);

        // Act
        Func<Task> act = async () => await _service.VerifyRegistrationAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("OTP is invalid or has expired.");
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvalidOtp);

        _output.WriteLine($"TCID03 PASSED: Threw expected AppException 400: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: OTP sai, số lần thử < tối đa -> Tăng số lần thử, ném 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_VerifyRegistration_IncorrectOtp_UnderMaxAttempts_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new VerifyRegistrationRequest
        {
            Email = ValidEmail,
            Otp = IncorrectOtp
        };

        var normalizedEmail = EmailAddressHelper.Normalize(ValidEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var pendingKey = RedisKeys.AuthPendingRegistration(subjectHash);
        var otpKey = RedisKeys.AuthOtp("registration", subjectHash);
        var attemptsKey = RedisKeys.AuthOtpAttempts("registration", subjectHash);

        var pending = new PendingRegistration(normalizedEmail, "hashed-pw", "Nguyen Van A");
        var rawOtpRecord = CreateOtpRecordJson("registration", normalizedEmail, ValidOtp, Salt);

        _redisMock.Setup(r => r.GetJsonAsync<PendingRegistration>(pendingKey))
            .ReturnsAsync(pending);
        _redisMock.Setup(r => r.GetStringAsync(otpKey))
            .ReturnsAsync(rawOtpRecord);
        _redisMock.Setup(r => r.IncrementAsync(attemptsKey, It.IsAny<TimeSpan>()))
            .ReturnsAsync(1); // 1st attempt < 5

        // Act
        Func<Task> act = async () => await _service.VerifyRegistrationAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("OTP is invalid or has expired.");
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvalidOtp);

        _redisMock.Verify(r => r.IncrementAsync(attemptsKey, It.IsAny<TimeSpan>()), Times.Once);

        _output.WriteLine($"TCID04 PASSED: Threw expected AppException 400: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID05: OTP sai, số lần thử >= 5 -> Xóa OTP & pending data, ném 400 (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_VerifyRegistration_IncorrectOtp_ExceedsMaxAttempts_RemovesPendingAndThrows_Abnormal()
    {
        // Arrange
        var request = new VerifyRegistrationRequest
        {
            Email = ValidEmail,
            Otp = IncorrectOtp
        };

        var normalizedEmail = EmailAddressHelper.Normalize(ValidEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var pendingKey = RedisKeys.AuthPendingRegistration(subjectHash);
        var otpKey = RedisKeys.AuthOtp("registration", subjectHash);
        var attemptsKey = RedisKeys.AuthOtpAttempts("registration", subjectHash);

        var pending = new PendingRegistration(normalizedEmail, "hashed-pw", "Nguyen Van A");
        var rawOtpRecord = CreateOtpRecordJson("registration", normalizedEmail, ValidOtp, Salt);

        _redisMock.Setup(r => r.GetJsonAsync<PendingRegistration>(pendingKey))
            .ReturnsAsync(pending);
        _redisMock.Setup(r => r.GetStringAsync(otpKey))
            .ReturnsAsync(rawOtpRecord);
        _redisMock.Setup(r => r.IncrementAsync(attemptsKey, It.IsAny<TimeSpan>()))
            .ReturnsAsync(5); // 5th attempt >= 5
        _redisMock.Setup(r => r.RemoveAsync(It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);

        // Act
        Func<Task> act = async () => await _service.VerifyRegistrationAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Too many incorrect OTP attempts. Request a new code.");
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvalidOtp);

        _redisMock.Verify(r => r.RemoveAsync(It.Is<string[]>(keys => keys.Contains(pendingKey) && keys.Contains(otpKey))), Times.Once);

        _output.WriteLine($"TCID05 PASSED: Threw expected AppException 400: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID06: Email đã tồn tại trong DB -> Xóa pending và ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID06_VerifyRegistration_EmailAlreadyRegistered_ThrowsConflict_Abnormal()
    {
        // Arrange
        var request = new VerifyRegistrationRequest
        {
            Email = ExistingEmail,
            Otp = ValidOtp
        };

        var normalizedEmail = EmailAddressHelper.Normalize(ExistingEmail);
        var subjectHash = HashingHelper.ComputeSha256Hex(normalizedEmail);
        var pendingKey = RedisKeys.AuthPendingRegistration(subjectHash);
        var otpKey = RedisKeys.AuthOtp("registration", subjectHash);
        var attemptsKey = RedisKeys.AuthOtpAttempts("registration", subjectHash);

        var pending = new PendingRegistration(normalizedEmail, "hashed-pw", "Nguyen Van A");
        var rawOtpRecord = CreateOtpRecordJson("registration", normalizedEmail, ValidOtp, Salt);

        _redisMock.Setup(r => r.GetJsonAsync<PendingRegistration>(pendingKey))
            .ReturnsAsync(pending);
        _redisMock.Setup(r => r.GetStringAsync(otpKey))
            .ReturnsAsync(rawOtpRecord);
        _redisMock.Setup(r => r.DeleteIfValueMatchesAsync(otpKey, rawOtpRecord))
            .ReturnsAsync(true);
        _redisMock.Setup(r => r.RemoveAsync(attemptsKey))
            .Returns(Task.CompletedTask);
        _redisMock.Setup(r => r.RemoveAsync(pendingKey))
            .Returns(Task.CompletedTask);

        _authRepoMock.Setup(r => r.EmailExistsAsync(normalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Account already exists

        // Act
        Func<Task> act = async () => await _service.VerifyRegistrationAsync(request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("An account with this email already exists.");

        _redisMock.Verify(r => r.RemoveAsync(pendingKey), Times.Once);

        _output.WriteLine($"TCID06 PASSED: Threw expected AppException 409: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID07: Validator - Email sai định dạng -> Validation fails on Email (Abnormal - A)
    // =========================================================================
    [Fact]
    public void TCID07_VerifyRegistration_InvalidEmailFormat_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new VerifyRegistrationRequest
        {
            Email = InvalidEmail,
            Otp = ValidOtp
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(VerifyRegistrationRequest.Email));

        _output.WriteLine($"TCID07 PASSED: Validation failed on Email as expected: {result.Errors.First(e => e.PropertyName == "Email").ErrorMessage}");
    }

    // =========================================================================
    // TCID08: Validator - OTP không đúng 6 số -> Validation fails on Otp (Boundary - B)
    // =========================================================================
    [Fact]
    public void TCID08_VerifyRegistration_InvalidOtpLengthOrFormat_ValidationFails_Boundary()
    {
        // Arrange
        var request = new VerifyRegistrationRequest
        {
            Email = ValidEmail,
            Otp = ShortOtp // 5 digits instead of 6
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(VerifyRegistrationRequest.Otp));

        _output.WriteLine($"TCID08 PASSED: Validation failed on Otp as expected: {result.Errors.First(e => e.PropertyName == "Otp").ErrorMessage}");
    }
}
