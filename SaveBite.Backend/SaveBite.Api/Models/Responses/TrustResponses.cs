namespace SaveBite.Backend.Models.Responses;

// ---------------------------------------------------------------------
// Shared
// ---------------------------------------------------------------------

public sealed record TrustLevelSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    int Rank);

public sealed record TrustLevelRequirementResponse(
    int MinScore,
    int MinCompletedOrders,
    decimal MinTotalOrderValue);

/// <summary>One measurable criterion with the customer's progress towards it.</summary>
public sealed record ProgressMetricResponse(
    string Key,
    decimal Current,
    decimal Required,
    decimal Remaining,
    decimal Percent);

// ---------------------------------------------------------------------
// Customer: UC-44 / UC-45 / UC-47
// ---------------------------------------------------------------------

public sealed record TrustScoreOverviewResponse(
    int CurrentScore,
    TrustLevelSummaryResponse Level,
    TrustLevelRequirementResponse? Requirement,
    bool IsHighestLevel,
    bool IsOfflineLockedPermanently,
    int CompletedOrdersCount,
    decimal AccumulatedOrderValue,
    int ActivePenaltyCount,
    string Standing,
    DateTime UpdatedAt);

public sealed record LevelProgressResponse(
    TrustLevelSummaryResponse CurrentLevel,
    TrustLevelSummaryResponse? NextLevel,
    bool IsHighestLevel,
    bool RequirementsMet,
    decimal OverallPercent,
    IReadOnlyList<ProgressMetricResponse> Metrics,
    int? ScoreDeltaPerCompletedOrder);

public sealed record TrustLevelInfoResponse(
    Guid Id,
    string Name,
    string? Description,
    int Rank,
    TrustLevelRequirementResponse? Requirement,
    bool IsCurrent,
    bool IsUnlocked);

// ---------------------------------------------------------------------
// Customer: UC-46
// ---------------------------------------------------------------------

public sealed record TrustScoreHistoryItemResponse(
    Guid Id,
    string EventType,
    int ScoreBefore,
    int ScoreDelta,
    int ScoreAfter,
    string? Reason,
    Guid? OrderId,
    string? OrderCode,
    DateTime CreatedAt);

// ---------------------------------------------------------------------
// Customer: UC-48 / UC-49
// ---------------------------------------------------------------------

public sealed record PenaltyItemResponse(
    Guid Id,
    Guid OrderId,
    string? OrderCode,
    int OccurrenceNumber,
    int ScoreDeducted,
    bool PermanentOfflineLock,
    DateTime? LockEndsAt,
    string Status,
    int RequiredOnlineOrders,
    decimal RequiredOnlineOrderValue,
    int CompletedOnlineOrders,
    decimal CompletedOnlineOrderValue,
    DateTime CreatedAt,
    DateTime? RecoveredAt);

public sealed record PenaltyRecoveryOrderResponse(
    Guid OrderId,
    string? OrderCode,
    decimal OrderValue,
    DateTime CreatedAt);

public sealed record PenaltyRecoveryResponse(
    PenaltyItemResponse Penalty,
    bool IsRecovered,
    decimal OverallPercent,
    IReadOnlyList<ProgressMetricResponse> Metrics,
    IReadOnlyList<PenaltyRecoveryOrderResponse> RecoveryOrders);

// ---------------------------------------------------------------------
// Admin: UC-129 .. UC-138
// ---------------------------------------------------------------------

public sealed record AdminTrustLevelResponse(
    Guid Id,
    string Name,
    string? Description,
    int Rank,
    bool IsActive,
    TrustLevelRequirementResponse? Requirement,
    int UserCount,
    DateTime UpdatedAt);

public sealed record PenaltyRuleResponse(
    Guid Id,
    int MinimumOccurrence,
    int ScoreDeduction,
    int RequiredOnlineOrders,
    decimal RequiredOnlineOrderValue,
    bool PermanentOfflineLock,
    bool IsActive,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsCurrent,
    string? CreatedByName,
    DateTime CreatedAt);

public sealed record TrustScoreRuleResponse(
    Guid Id,
    int ScoreDelta,
    string Kind,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsCurrent,
    string? CreatedByName,
    DateTime CreatedAt);
