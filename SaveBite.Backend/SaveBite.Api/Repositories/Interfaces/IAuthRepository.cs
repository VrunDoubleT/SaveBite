using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IAuthRepository
{
    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddUserAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetRefreshTokenWithUserAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    void AddRefreshToken(RefreshToken refreshToken);

    Task RevokeAllActiveRefreshTokensAsync(
        Guid userId,
        string reason,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IRepositoryTransaction> BeginSerializableTransactionAsync(
        CancellationToken cancellationToken = default);
}
