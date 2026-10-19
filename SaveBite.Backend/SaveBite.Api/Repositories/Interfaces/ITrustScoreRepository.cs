using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Repositories.Interfaces;

/// <summary>Customer-side read access to Trust Score data (UC-44 .. UC-49).</summary>
public interface ITrustScoreRepository
{
    Task<UserTrustScore?> GetUserTrustScoreAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserLevelProgress?> GetLevelProgressAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Active levels with their requirement, ordered by rank ascending.</summary>
    Task<IReadOnlyList<TrustLevel>> GetActiveLevelsAsync(CancellationToken cancellationToken = default);

    Task<TrustLevel?> GetLevelWithRequirementAsync(Guid levelId, CancellationToken cancellationToken = default);

    Task<PagedResult<TrustScoreHistory>> GetHistoryPagedAsync(
        Guid userId, GetTrustScoreHistoryRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<NoShowPenalty>> GetPenaltiesPagedAsync(
        Guid userId, GetPenaltiesRequest request, CancellationToken cancellationToken = default);

    Task<NoShowPenalty?> GetPenaltyWithRecoveryOrdersAsync(
        Guid userId, Guid penaltyId, CancellationToken cancellationToken = default);

    Task<int> CountRecoveringPenaltiesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>The score rule currently in effect that awards points (ScoreDelta &gt; 0).</summary>
    Task<TrustScoreRule?> GetCurrentEarningRuleAsync(DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the Trust Score, level progress and initial level history rows for a user that has none.
    /// Returns false when another request created them first.
    /// </summary>
    Task<bool> TryProvisionAsync(
        Guid userId, Guid initialLevelId, int initialScore, DateTime now, CancellationToken cancellationToken = default);
}
