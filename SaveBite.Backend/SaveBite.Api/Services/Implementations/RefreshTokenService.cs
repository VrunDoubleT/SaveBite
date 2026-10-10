using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public class RefreshTokenService : IRefreshTokenService
{
    private const string ReuseReason = "Refresh token reuse detected";

    private readonly IAuthRepository _authRepository;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _settings;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenService(
        IAuthRepository authRepository,
        IJwtService jwtService,
        IOptions<JwtSettings> options,
        TimeProvider timeProvider)
    {
        _authRepository = authRepository;
        _jwtService = jwtService;
        _settings = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<TokenPairResponse> IssueAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        EnsureAccountCanAuthenticate(user);

        var now = UtcNow();
        var (rawToken, entity) = CreateRefreshToken(user.Id, now);

        _authRepository.AddRefreshToken(entity);
        await _authRepository.SaveChangesAsync(cancellationToken);

        return CreateTokenPair(user, rawToken, entity.ExpiresAt);
    }

    public async Task<TokenPairResponse> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw AppException.Unauthorized("Refresh token is invalid.");

        // Rotation runs in a serializable transaction so a token can have only.
        // One successful successor, even when requests arrive concurrently.
        await using var transaction =
            await _authRepository.BeginSerializableTransactionAsync(cancellationToken);

        var tokenHash = HashToken(refreshToken);
        var currentToken = await _authRepository.GetRefreshTokenWithUserAsync(
            tokenHash,
            cancellationToken);

        if (currentToken is null)
            throw AppException.Unauthorized("Refresh token is invalid.");

        var now = UtcNow();

        if (currentToken.RevokedAt is not null)
        {
            // Reusing a revoked token may indicate theft. Revoke every active.
            // Session for the account before requiring a new sign-in.
            await RevokeAllActiveTokensAsync(
                currentToken.UserId,
                ReuseReason,
                now,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            throw AppException.Unauthorized(
                "Refresh token reuse was detected. Please sign in again.");
        }

        if (currentToken.ExpiresAt <= now)
        {
            currentToken.RevokedAt = now;
            currentToken.RevokeReason = "Expired";
            await _authRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            throw AppException.Unauthorized("Refresh token has expired.");
        }

        if (currentToken.User.Status != UserStatus.Active)
        {
            await RevokeAllActiveTokensAsync(
                currentToken.UserId,
                "Account suspended",
                now,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            throw AppException.Unauthorized("Account is not active.");
        }

        var (newRawToken, newToken) = CreateRefreshToken(currentToken.UserId, now);

        currentToken.RevokedAt = now;
        currentToken.RevokeReason = "Rotated";
        currentToken.ReplacedByTokenId = newToken.Id;

        _authRepository.AddRefreshToken(newToken);
        await _authRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CreateTokenPair(
            currentToken.User,
            newRawToken,
            newToken.ExpiresAt);
    }

    public async Task RevokeAsync(
        string refreshToken,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash = HashToken(refreshToken);
        var token = await _authRepository.GetRefreshTokenWithUserAsync(
            tokenHash,
            cancellationToken);

        if (token is null || token.RevokedAt is not null)
            return;

        token.RevokedAt = UtcNow();
        token.RevokeReason = NormalizeReason(reason);

        await _authRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await RevokeAllActiveTokensAsync(
            userId,
            reason,
            UtcNow(),
            cancellationToken);
    }

    private async Task RevokeAllActiveTokensAsync(
        Guid userId,
        string reason,
        DateTime revokedAt,
        CancellationToken cancellationToken)
    {
        var normalizedReason = NormalizeReason(reason);

        await _authRepository.RevokeAllActiveRefreshTokensAsync(
            userId,
            normalizedReason,
            revokedAt,
            cancellationToken);
    }

    private (string RawToken, RefreshToken Entity) CreateRefreshToken(
        Guid userId,
        DateTime now)
    {
        var rawToken = WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(64));

        return (
            rawToken,
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = HashToken(rawToken),
                ExpiresAt = now.AddDays(
                    _settings.RefreshTokenLifetimeDays),
                CreatedAt = now
            });
    }

    private TokenPairResponse CreateTokenPair(
        User user,
        string refreshToken,
        DateTime refreshTokenExpiresAt)
    {
        var accessToken = _jwtService.CreateAccessToken(user);

        return new TokenPairResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken,
            refreshTokenExpiresAt);
    }

    private static string HashToken(string token)
        // Only the hash is persisted; a leaked database cannot reveal live tokens.
        => Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(token)))
            .ToLowerInvariant();

    private static string NormalizeReason(string reason)
    {
        var normalized = string.IsNullOrWhiteSpace(reason)
            ? "Revoked"
            : reason.Trim();

        return normalized[..Math.Min(normalized.Length, 200)];
    }

    private static void EnsureAccountCanAuthenticate(User user)
    {
        if (user.Status != UserStatus.Active)
            throw AppException.Unauthorized("Account is not active.");
    }

    private DateTime UtcNow()
        => _timeProvider.GetUtcNow().UtcDateTime;
}
