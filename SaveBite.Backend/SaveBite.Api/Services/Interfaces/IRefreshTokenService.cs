using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IRefreshTokenService
{
    Task<TokenPairResponse> IssueAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<TokenPairResponse> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        string refreshToken,
        string reason,
        CancellationToken cancellationToken = default);

    Task RevokeAllAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);
}
