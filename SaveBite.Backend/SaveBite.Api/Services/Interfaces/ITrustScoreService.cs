using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface ITrustScoreService
{
    // Initialize the lowest active level without resetting an existing customer.
    Task InitializeAsync(Guid userId, CancellationToken cancellationToken = default);

    // UC-44
    Task<TrustScoreOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default);

    // UC-45
    Task<LevelProgressResponse> GetLevelProgressAsync(Guid userId, CancellationToken cancellationToken = default);

    // UC-46
    Task<PagedResult<TrustScoreHistoryItemResponse>> GetHistoryAsync(
        Guid userId, GetTrustScoreHistoryRequest request, CancellationToken cancellationToken = default);

    // UC-47
    Task<IReadOnlyList<TrustLevelInfoResponse>> GetLevelsAsync(Guid userId, CancellationToken cancellationToken = default);

    // UC-48
    Task<PagedResult<PenaltyItemResponse>> GetPenaltiesAsync(
        Guid userId, GetPenaltiesRequest request, CancellationToken cancellationToken = default);

    // UC-49
    Task<PenaltyRecoveryResponse> GetPenaltyRecoveryAsync(
        Guid userId, Guid penaltyId, CancellationToken cancellationToken = default);
}
