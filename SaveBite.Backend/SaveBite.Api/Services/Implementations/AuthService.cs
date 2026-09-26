using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SaveBite.Backend.Caching;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.EmailTemplates;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private const string RegistrationPurpose = "registration";
    private const string PasswordResetPurpose = "password-reset";
    private readonly IAuthRepository _authRepository;
    private readonly IAuthRedisService _redis;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly byte[] _otpHashKey;

    public AuthService(
        IAuthRepository authRepository,
        IAuthRedisService redis,
        IPasswordHasherService passwordHasher,
        IRefreshTokenService refreshTokenService,
        IRabbitMqPublisher publisher,
        ILogger<AuthService> logger,
        IOptions<AuthSettings> authOptions,
        IOptions<JwtSettings> jwtOptions,
        TimeProvider timeProvider)
    {
        _authRepository = authRepository;
        _redis = redis;
        _passwordHasher = passwordHasher;
        _refreshTokenService = refreshTokenService;
        _publisher = publisher;
        _logger = logger;
        _settings = authOptions.Value;
        _timeProvider = timeProvider;
        _otpHashKey = Encoding.UTF8.GetBytes(jwtOptions.Value.SigningKey);
    }

    public async Task RequestRegistrationAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = EmailAddressHelper.Normalize(request.Email);

        if (await _authRepository.EmailExistsAsync(email, cancellationToken))
        {
            _logger.LogWarning(
                "Registration rejected because the account already exists. SubjectHash: {SubjectHash}",
                HashingHelper.ComputeSha256Hex(email));
            throw AppException.Conflict("An account with this email already exists.");
        }

        var subjectHash = HashingHelper.ComputeSha256Hex(email);
        await EnsureOtpRequestAllowedAsync(
            RegistrationPurpose,
            subjectHash);

        var expiry = OtpLifetime();

        // Keep unverified registration data out of PostgreSQL. Redis only stores
        // the password hash and deletes the pending data automatically on expiry.
        var pending = new PendingRegistration(
            email,
            _passwordHasher.HashPassword(request.Password),
            request.FullName.Trim());

        await _redis.SetJsonAsync(
            RedisKeys.AuthPendingRegistration(subjectHash),
            pending,
            expiry);

        await CreateAndPublishOtpAsync(
            RegistrationPurpose,
            email,
            subjectHash,
            "Verify your SaveBite account",
            cancellationToken);
    }

    public async Task VerifyRegistrationAsync(
        VerifyRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = EmailAddressHelper.Normalize(request.Email);
        var subjectHash = HashingHelper.ComputeSha256Hex(email);
        var pendingKey = RedisKeys.AuthPendingRegistration(subjectHash);
        var pending = await _redis.GetJsonAsync<PendingRegistration>(pendingKey);

        if (pending is null)
        {
            _logger.LogWarning(
                "Registration verification failed because pending data was not found. SubjectHash: {SubjectHash}",
                subjectHash);
            throw InvalidOtp("Registration data or OTP has expired.");
        }

        await VerifyAndConsumeOtpAsync(
            RegistrationPurpose,
            email,
            subjectHash,
            request.Otp,
            pendingKey);

        if (await _authRepository.EmailExistsAsync(email, cancellationToken))
        {
            _logger.LogWarning(
                "Registration verification found an existing account. SubjectHash: {SubjectHash}",
                subjectHash);
            await _redis.RemoveAsync(pendingKey);
            throw AppException.Conflict("An account with this email already exists.");
        }

        var now = UtcNow();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = pending.Email,
            PasswordHash = pending.PasswordHash,
            FullName = pending.FullName,
            EmailVerifiedAt = now,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            Role = UserRole.User,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            if (!await _authRepository.TryAddUserAsync(user, cancellationToken))
                throw AppException.Conflict(
                    "An account with this email already exists.");
        }
        finally
        {
            await _redis.RemoveAsync(pendingKey);
        }

        _logger.LogInformation(
            "Registration completed. UserId: {UserId}",
            user.Id);
    }

    public async Task<TokenPairResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = EmailAddressHelper.Normalize(request.Email);
        var subjectHash = HashingHelper.ComputeSha256Hex(email);
        var emailFailureKey = RedisKeys.AuthLoginFailuresByEmail(subjectHash);
        await EnsureLoginNotBlockedAsync(emailFailureKey, subjectHash);

        var user = await _authRepository.GetUserByEmailAsync(
            email,
            cancellationToken);

        if (user is null ||
            !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            var failureCount = await RecordLoginFailureAsync(emailFailureKey);
            _logger.LogWarning(
                "Login failed. SubjectHash: {SubjectHash}, FailureCount: {FailureCount}",
                subjectHash,
                failureCount);
            throw AppException.Unauthorized("Email or password is incorrect.");
        }

        if (user.EmailVerifiedAt is null)
        {
            _logger.LogWarning(
                "Login rejected because the email is not verified. UserId: {UserId}",
                user.Id);
            throw AppException.Unauthorized("Email address has not been verified.");
        }

        await _redis.RemoveAsync(emailFailureKey);
        _logger.LogInformation("Login succeeded. UserId: {UserId}", user.Id);
        return await _refreshTokenService.IssueAsync(
            user,
            cancellationToken);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserByIdAsync(
            userId,
            cancellationToken);

        if (user is null)
            throw AppException.Unauthorized();

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.Phone,
            user.FullName,
            user.AvatarUrl,
            user.Role.ToString());
    }

    public async Task RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = EmailAddressHelper.Normalize(request.Email);
        var subjectHash = HashingHelper.ComputeSha256Hex(email);
        await EnsureOtpRequestAllowedAsync(
            PasswordResetPurpose,
            subjectHash);

        var exists = await _authRepository.EmailExistsAsync(
            email,
            cancellationToken);

        // Return the same response for unknown addresses to prevent account discovery.
        if (!exists)
        {
            _logger.LogDebug(
                "Password reset requested for an unknown account. SubjectHash: {SubjectHash}",
                subjectHash);
            return;
        }

        var pendingKey = RedisKeys.AuthPendingPasswordReset(subjectHash);
        var pending = new PendingPasswordReset(
            _passwordHasher.HashPassword(request.NewPassword));

        // The plain password is never cached. The pending hash expires together
        // with the OTP and is applied only after successful verification.
        await _redis.SetJsonAsync(pendingKey, pending, OtpLifetime());

        try
        {
            await CreateAndPublishOtpAsync(
                PasswordResetPurpose,
                email,
                subjectHash,
                "Reset your SaveBite password",
                cancellationToken);
        }
        catch
        {
            await _redis.RemoveAsync(pendingKey);
            throw;
        }
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = EmailAddressHelper.Normalize(request.Email);
        var subjectHash = HashingHelper.ComputeSha256Hex(email);
        var pendingKey = RedisKeys.AuthPendingPasswordReset(subjectHash);
        var pending = await _redis.GetJsonAsync<PendingPasswordReset>(pendingKey);

        if (pending is null)
        {
            _logger.LogWarning(
                "Password reset verification failed because pending data was not found. SubjectHash: {SubjectHash}",
                subjectHash);
            throw InvalidOtp("Password-reset data or OTP has expired.");
        }

        // The OTP is consumed before changing the password, so it cannot be replayed.
        await VerifyAndConsumeOtpAsync(
            PasswordResetPurpose,
            email,
            subjectHash,
            request.Otp,
            pendingKey);

        try
        {
            var user = await _authRepository.GetUserByEmailAsync(
                email,
                cancellationToken);
            if (user is null)
            {
                _logger.LogError(
                    "Password reset account disappeared after OTP verification. SubjectHash: {SubjectHash}",
                    subjectHash);
                throw InvalidOtp();
            }

            user.PasswordHash = pending.PasswordHash;
            user.UpdatedAt = UtcNow();
            await _authRepository.SaveChangesAsync(cancellationToken);
            await _refreshTokenService.RevokeAllAsync(
                user.Id,
                "Password reset",
                cancellationToken);

            _logger.LogInformation(
                "Password reset completed and active sessions were revoked. UserId: {UserId}",
                user.Id);
        }
        finally
        {
            // Verification is single-use, so its pending password hash is too.
            await _redis.RemoveAsync(pendingKey);
        }
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserByIdAsync(
            userId,
            cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "Password change rejected because the user was not found. UserId: {UserId}",
                userId);
            throw AppException.Unauthorized();
        }

        if (!_passwordHasher.VerifyPassword(
                request.CurrentPassword,
                user.PasswordHash))
        {
            _logger.LogWarning(
                "Password change rejected because the current password was incorrect. UserId: {UserId}",
                userId);
            throw AppException.Unauthorized("Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = UtcNow();
        await _authRepository.SaveChangesAsync(cancellationToken);
        await _refreshTokenService.RevokeAllAsync(
            user.Id,
            "Password changed",
            cancellationToken);

        _logger.LogInformation(
            "Password changed and active sessions were revoked. UserId: {UserId}",
            user.Id);
    }

    private async Task CreateAndPublishOtpAsync(
        string purpose,
        string email,
        string subjectHash,
        string subject,
        CancellationToken cancellationToken)
    {
        // Generate exactly six digits, including values with leading zeroes.
        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var record = new OtpRecord(salt, HashOtp(purpose, email, otp, salt));
        var key = RedisKeys.AuthOtp(purpose, subjectHash);

        // Store only a keyed hash of the OTP. The plain code exists only long
        // enough to be placed on the email queue.
        await _redis.SetStringAsync(
            key,
            JsonSerializer.Serialize(record),
            OtpLifetime());
        await _redis.RemoveAsync(RedisKeys.AuthOtpAttempts(purpose, subjectHash));

        try
        {
            await _publisher.PublishAsync(
                RabbitMqRoutingKeys.EmailRoutingKey,
                new EmailNotificationEvent
                {
                    To = email,
                    Subject = subject,
                    Body = OtpEmailTemplate.Build(
                        otp,
                        _settings.OtpLifetimeMinutes),
                    IsHtml = true
                },
                cancellationToken);

            _logger.LogInformation(
                "OTP email event published. Purpose: {Purpose}, SubjectHash: {SubjectHash}",
                purpose,
                subjectHash);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish OTP email event. Purpose: {Purpose}, SubjectHash: {SubjectHash}",
                purpose,
                subjectHash);

            // Do not leave a valid code behind when the event was not accepted.
            await _redis.RemoveAsync(key);
            throw;
        }
    }

    private async Task VerifyAndConsumeOtpAsync(
        string purpose,
        string email,
        string subjectHash,
        string otp,
        string? relatedKey = null)
    {
        var otpKey = RedisKeys.AuthOtp(purpose, subjectHash);
        var attemptsKey = RedisKeys.AuthOtpAttempts(purpose, subjectHash);
        var rawRecord = await _redis.GetStringAsync(otpKey);
        if (rawRecord is null)
        {
            _logger.LogWarning(
                "OTP verification failed because the code was missing or expired. Purpose: {Purpose}, SubjectHash: {SubjectHash}",
                purpose,
                subjectHash);
            throw InvalidOtp();
        }

        // Treat malformed cached data as an invalid OTP instead of exposing a
        // serialization error through the authentication endpoint.
        OtpRecord? record;
        try
        {
            record = JsonSerializer.Deserialize<OtpRecord>(rawRecord);
        }
        catch (JsonException)
        {
            record = null;
        }

        if (record is null || !OtpMatches(record, purpose, email, otp))
        {
            // Invalidate both the OTP and its related registration after repeated
            // failures to limit online guessing attempts.
            var attempts = await _redis.IncrementAsync(attemptsKey, OtpLifetime());
            _logger.LogWarning(
                "OTP verification failed. Purpose: {Purpose}, SubjectHash: {SubjectHash}, Attempt: {Attempt}",
                purpose,
                subjectHash,
                attempts);

            if (attempts >= _settings.MaxOtpVerificationAttempts)
            {
                var keys = relatedKey is null
                    ? new[] { otpKey, attemptsKey }
                    : new[] { otpKey, attemptsKey, relatedKey };
                await _redis.RemoveAsync(keys);
                _logger.LogWarning(
                    "OTP invalidated after reaching the verification limit. Purpose: {Purpose}, SubjectHash: {SubjectHash}",
                    purpose,
                    subjectHash);
                throw InvalidOtp("Too many incorrect OTP attempts. Request a new code.");
            }

            throw InvalidOtp();
        }

        // Compare-and-delete is atomic; concurrent requests cannot reuse one OTP.
        if (!await _redis.DeleteIfValueMatchesAsync(otpKey, rawRecord))
        {
            _logger.LogWarning(
                "OTP verification lost a concurrent consume race. Purpose: {Purpose}, SubjectHash: {SubjectHash}",
                purpose,
                subjectHash);
            throw InvalidOtp();
        }

        await _redis.RemoveAsync(attemptsKey);
        _logger.LogInformation(
            "OTP verified and consumed. Purpose: {Purpose}, SubjectHash: {SubjectHash}",
            purpose,
            subjectHash);
    }

    private async Task EnsureOtpRequestAllowedAsync(
        string purpose,
        string subjectHash)
    {
        var decision = await _redis.AcquireOtpRequestPermitAsync(
            purpose,
            subjectHash,
            TimeSpan.FromSeconds(_settings.OtpResendCooldownSeconds),
            _settings.MaxOtpRequestsPerEmailPerDay,
            UntilNextUtcDay());

        if (!decision.Allowed)
        {
            // Redis returns the remaining TTL so clients receive a useful retry delay.
            var seconds = Math.Max(
                1,
                (int)Math.Ceiling(decision.RetryAfter?.TotalSeconds ?? 60));
            _logger.LogWarning(
                "OTP request rate limit reached. Purpose: {Purpose}, SubjectHash: {SubjectHash}, RetryAfterSeconds: {RetryAfterSeconds}",
                purpose,
                subjectHash,
                seconds);
            throw AppException.TooManyRequests(
                $"Too many OTP requests. Try again in {seconds} seconds.");
        }
    }

    private async Task EnsureLoginNotBlockedAsync(
        string emailFailureKey,
        string subjectHash)
    {
        var emailFailures = await ReadCounterAsync(emailFailureKey);
        if (emailFailures < _settings.MaxLoginAttemptsPerEmail)
        {
            return;
        }

        var retryAfter = await _redis.GetTimeToLiveAsync(emailFailureKey);
        var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter?.TotalSeconds ?? 60));
        _logger.LogWarning(
            "Login temporarily blocked by the failure limit. SubjectHash: {SubjectHash}, RetryAfterSeconds: {RetryAfterSeconds}",
            subjectHash,
            seconds);
        throw AppException.TooManyRequests(
            $"Too many failed login attempts. Try again in {seconds} seconds.");
    }

    private async Task<long> RecordLoginFailureAsync(
        string emailFailureKey)
    {
        // All failures within this fixed Redis TTL contribute to the same window.
        var expiry = TimeSpan.FromMinutes(_settings.LoginAttemptWindowMinutes);
        return await _redis.IncrementAsync(emailFailureKey, expiry);
    }

    private async Task<long> ReadCounterAsync(string key)
    {
        // Missing and malformed counters are treated as zero.
        var value = await _redis.GetStringAsync(key);
        return long.TryParse(value, out var count) ? count : 0;
    }

    private bool OtpMatches(
        OtpRecord record,
        string purpose,
        string email,
        string otp)
    {
        // Compare fixed-size hashes in constant time to avoid timing leaks.
        var actual = Encoding.ASCII.GetBytes(
            HashOtp(purpose, email, otp, record.Salt));
        var expected = Encoding.ASCII.GetBytes(record.Hash);
        return actual.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private string HashOtp(
        string purpose,
        string email,
        string otp,
        string salt)
    {
        // Purpose, account and random salt bind the code to one exact workflow.
        using var hmac = new HMACSHA256(_otpHashKey);
        return Convert.ToHexString(
            hmac.ComputeHash(
                Encoding.UTF8.GetBytes($"{purpose}:{email}:{salt}:{otp}")));
    }

    private TimeSpan OtpLifetime()
        // Pending data and OTP records use the same configured lifetime.
        => TimeSpan.FromMinutes(_settings.OtpLifetimeMinutes);

    private TimeSpan UntilNextUtcDay()
    {
        // Daily rate-limit counters expire at the next UTC date boundary.
        var now = _timeProvider.GetUtcNow();
        var nextDay = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
        return nextDay - now;
    }

    // TimeProvider keeps time-dependent authentication behavior testable.
    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private static AppException InvalidOtp(
        string message = "OTP is invalid or has expired.")
        // Use one public error code for invalid, expired and already-consumed OTPs.
        => AppException.BadRequest(message, ErrorCodes.InvalidOtp);
}
