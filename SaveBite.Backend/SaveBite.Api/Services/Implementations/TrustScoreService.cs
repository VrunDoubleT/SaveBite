using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class TrustScoreService : ITrustScoreService
{
    // Starting score of a customer that has no Trust Score yet (matches the seeded data and SRS UC-44).
    private const int InitialScore = 100;

    public const string StandingGood = "GoodStanding";
    public const string StandingRestricted = "Restricted";
    public const string StandingOfflineLocked = "OfflineLocked";

    private readonly ITrustScoreRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TrustScoreService> _logger;

    public TrustScoreService(
        ITrustScoreRepository repository,
        TimeProvider timeProvider,
        ILogger<TrustScoreService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // UC-44: View Trust Score and Level.
    public async Task<TrustScoreOverviewResponse> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (score, progress) = await EnsureProvisionedAsync(userId, cancellationToken);
        var levels = await _repository.GetActiveLevelsAsync(cancellationToken);
        var activePenalties = await _repository.CountRecoveringPenaltiesAsync(userId, cancellationToken);

        var standing = score.IsOfflineLockedPermanently
            ? StandingOfflineLocked
            : activePenalties > 0 ? StandingRestricted : StandingGood;

        return new TrustScoreOverviewResponse(
            score.CurrentScore,
            ToSummary(score.TrustLevel),
            ToRequirement(score.TrustLevel.Requirement),
            !levels.Any(level => level.Rank > score.TrustLevel.Rank),
            score.IsOfflineLockedPermanently,
            progress.CompletedOrdersCount,
            progress.AccumulatedOrderValue,
            activePenalties,
            standing,
            score.UpdatedAt);
    }

    // UC-45: Track Level Progress.
    public async Task<LevelProgressResponse> GetLevelProgressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (score, progress) = await EnsureProvisionedAsync(userId, cancellationToken);
        var levels = await _repository.GetActiveLevelsAsync(cancellationToken);
        var earningRule = await _repository.GetCurrentEarningRuleAsync(_timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        var currentLevel = ToSummary(score.TrustLevel);
        var nextLevel = levels.FirstOrDefault(level => level.Rank > score.TrustLevel.Rank);

        if (nextLevel is null)
        {
            return new LevelProgressResponse(
                currentLevel, null, true, false, 100m, Array.Empty<ProgressMetricResponse>(), earningRule?.ScoreDelta);
        }

        var requirement = nextLevel.Requirement;
        var metrics = new List<ProgressMetricResponse>
        {
            BuildMetric("score", score.CurrentScore, requirement?.MinScore ?? 0),
            BuildMetric("completedOrders", progress.CompletedOrdersCount, requirement?.MinCompletedOrders ?? 0),
            BuildMetric("orderValue", progress.AccumulatedOrderValue, requirement?.MinTotalOrderValue ?? 0)
        };

        return new LevelProgressResponse(
            currentLevel,
            ToSummary(nextLevel),
            false,
            metrics.All(metric => metric.Remaining == 0),
            AveragePercent(metrics),
            metrics,
            earningRule?.ScoreDelta);
    }

    // UC-46: View Trust Score History.
    public async Task<PagedResult<TrustScoreHistoryItemResponse>> GetHistoryAsync(
        Guid userId, GetTrustScoreHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var paged = await _repository.GetHistoryPagedAsync(userId, request, cancellationToken);

        var items = paged.Items.Select(history => new TrustScoreHistoryItemResponse(
            history.Id,
            history.EventType.ToString(),
            history.ScoreBefore,
            history.ScoreDelta,
            history.ScoreAfter,
            history.Reason,
            history.OrderId,
            history.Order?.OrderCode,
            history.CreatedAt)).ToList();

        return PagedResult<TrustScoreHistoryItemResponse>.Create(items, paged.Page, paged.PageSize, paged.TotalItems);
    }

    // UC-47: View Level Info & Benefits.
    public async Task<IReadOnlyList<TrustLevelInfoResponse>> GetLevelsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (score, _) = await EnsureProvisionedAsync(userId, cancellationToken);
        var levels = await _repository.GetActiveLevelsAsync(cancellationToken);

        return levels.Select(level => new TrustLevelInfoResponse(
            level.Id,
            level.Name,
            level.Description,
            level.Rank,
            ToRequirement(level.Requirement),
            level.Id == score.TrustLevelId,
            level.Rank <= score.TrustLevel.Rank)).ToList();
    }

    // UC-48: View Penalty History.
    public async Task<PagedResult<PenaltyItemResponse>> GetPenaltiesAsync(
        Guid userId, GetPenaltiesRequest request, CancellationToken cancellationToken = default)
    {
        var paged = await _repository.GetPenaltiesPagedAsync(userId, request, cancellationToken);
        var items = paged.Items.Select(ToPenaltyItem).ToList();

        return PagedResult<PenaltyItemResponse>.Create(items, paged.Page, paged.PageSize, paged.TotalItems);
    }

    // UC-49: Track Penalty Recovery Progress.
    public async Task<PenaltyRecoveryResponse> GetPenaltyRecoveryAsync(
        Guid userId, Guid penaltyId, CancellationToken cancellationToken = default)
    {
        var penalty = await _repository.GetPenaltyWithRecoveryOrdersAsync(userId, penaltyId, cancellationToken);
        if (penalty is null)
            throw AppException.NotFound("Penalty not found.");

        var metrics = new List<ProgressMetricResponse>
        {
            BuildMetric("onlineOrders", penalty.CompletedOnlineOrders, penalty.RequiredOnlineOrders),
            BuildMetric("onlineOrderValue", penalty.CompletedOnlineOrderValue, penalty.RequiredOnlineOrderValue)
        };

        var isRecovered = penalty.Status == NoShowPenaltyStatus.Recovered;

        var recoveryOrders = penalty.RecoveryOrders
            .OrderBy(recovery => recovery.CreatedAt)
            .Select(recovery => new PenaltyRecoveryOrderResponse(
                recovery.OrderId,
                recovery.Order?.OrderCode,
                recovery.OrderValue,
                recovery.CreatedAt))
            .ToList();

        return new PenaltyRecoveryResponse(
            ToPenaltyItem(penalty),
            isRecovered,
            isRecovered ? 100m : AveragePercent(metrics),
            metrics,
            recoveryOrders);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    // Customers created before FE-15 (or by the registration flow) may have no Trust Score rows yet.
    // They are created on first access, starting on the lowest active level (BR-158).
    private async Task<(UserTrustScore Score, UserLevelProgress Progress)> EnsureProvisionedAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var score = await _repository.GetUserTrustScoreAsync(userId, cancellationToken);

        if (score is null)
        {
            var levels = await _repository.GetActiveLevelsAsync(cancellationToken);
            var lowestLevel = levels.FirstOrDefault();
            if (lowestLevel is null)
                throw AppException.ServiceUnavailable("Trust levels are not configured yet.");

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var created = await _repository.TryProvisionAsync(userId, lowestLevel.Id, InitialScore, now, cancellationToken);
            if (created)
                _logger.LogInformation("Trust Score provisioned for user {UserId}.", userId);

            score = await _repository.GetUserTrustScoreAsync(userId, cancellationToken)
                    ?? throw AppException.InternalError();
        }

        var progress = await _repository.GetLevelProgressAsync(userId, cancellationToken)
                       ?? new UserLevelProgress { UserId = userId };

        return (score, progress);
    }

    private static TrustLevelSummaryResponse ToSummary(TrustLevel level)
        => new(level.Id, level.Name, level.Description, level.Rank);

    private static TrustLevelRequirementResponse? ToRequirement(TrustLevelRequirement? requirement)
        => requirement is null
            ? null
            : new TrustLevelRequirementResponse(
                requirement.MinScore,
                requirement.MinCompletedOrders,
                requirement.MinTotalOrderValue);

    private static ProgressMetricResponse BuildMetric(string key, decimal current, decimal required)
    {
        var remaining = Math.Max(required - current, 0m);
        var percent = required <= 0m
            ? 100m
            : Math.Clamp(Math.Round(current / required * 100m, 1), 0m, 100m);

        return new ProgressMetricResponse(key, current, required, remaining, percent);
    }

    private static decimal AveragePercent(IReadOnlyCollection<ProgressMetricResponse> metrics)
        => metrics.Count == 0 ? 100m : Math.Round(metrics.Average(metric => metric.Percent), 1);

    private static PenaltyItemResponse ToPenaltyItem(NoShowPenalty penalty)
        => new(
            penalty.Id,
            penalty.OrderId,
            penalty.Order?.OrderCode,
            penalty.OccurrenceNumber,
            penalty.ScoreDeducted,
            penalty.PermanentOfflineLock,
            penalty.LockEndsAt,
            penalty.Status.ToString(),
            penalty.RequiredOnlineOrders,
            penalty.RequiredOnlineOrderValue,
            penalty.CompletedOnlineOrders,
            penalty.CompletedOnlineOrderValue,
            penalty.CreatedAt,
            penalty.RecoveredAt);
}
