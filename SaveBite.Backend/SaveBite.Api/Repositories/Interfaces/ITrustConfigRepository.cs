using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

/// <summary>Admin-side access to Trust Score configuration (UC-129 .. UC-138, UC-152 .. UC-156).</summary>
public interface ITrustConfigRepository
{
    // Levels
    Task<IReadOnlyList<TrustLevel>> GetLevelsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetUserCountsByLevelAsync(CancellationToken cancellationToken = default);

    Task<bool> LevelExistsAsync(Guid levelId, CancellationToken cancellationToken = default);

    Task<bool> LevelNameExistsAsync(string name, Guid? excludeLevelId = null, CancellationToken cancellationToken = default);

    Task<int> GetMaxLevelRankAsync(CancellationToken cancellationToken = default);

    Task<int> GetNextLevelHistoryVersionAsync(Guid levelId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrustLevelHistory>> GetLevelHistoriesAsync(Guid levelId, CancellationToken cancellationToken = default);

    /// <summary>Tracked level (with requirement and deposit policy) for update.</summary>
    Task<TrustLevel?> GetLevelForUpdateAsync(Guid levelId, CancellationToken cancellationToken = default);

    /// <summary>Tracked active levels (with requirement) ordered by rank, used to validate thresholds.</summary>
    Task<IReadOnlyList<TrustLevel>> GetActiveLevelsForValidationAsync(CancellationToken cancellationToken = default);

    /// <summary>Tracked all levels (with requirement) ordered by rank, used when creating a new top level.</summary>
    Task<IReadOnlyList<TrustLevel>> GetAllLevelsWithRequirementsAsync(CancellationToken cancellationToken = default);

    /// <summary>Tracked UserTrustScore rows currently assigned to a specific level.</summary>
    Task<IReadOnlyList<UserTrustScore>> GetUserScoresByLevelForUpdateAsync(Guid levelId, CancellationToken cancellationToken = default);

    /// <summary>Read UserLevelProgress keyed by UserId for a set of users.</summary>
    Task<IReadOnlyDictionary<Guid, UserLevelProgress>> GetUserProgressMapAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    void AddLevel(TrustLevel level);

    void AddRequirement(TrustLevelRequirement requirement);

    void AddDepositPolicy(TrustLevelDepositPolicy policy);

    void AddDepositRanges(IEnumerable<TrustLevelDepositRange> ranges);

    void RemoveDepositRanges(IEnumerable<TrustLevelDepositRange> ranges);

    void AddLevelHistory(TrustLevelHistory history);

    void AddUserTrustLevelHistories(IEnumerable<UserTrustLevelHistory> histories);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        bool lockLevelsAndScores = false,
        CancellationToken cancellationToken = default);

    // Penalty rules
    Task<IReadOnlyList<RecoveryRequirementRule>> GetPenaltyRulesAsync(CancellationToken cancellationToken = default);

    Task<RecoveryRequirementRule?> GetPenaltyRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds any rule with the same MinimumOccurrence whose [EffectiveFrom, EffectiveTo) interval
    /// overlaps with [from, to).
    /// </summary>
    Task<RecoveryRequirementRule?> FindOverlappingPenaltyRuleAsync(
        int minimumOccurrence,
        DateTime from,
        DateTime? to,
        Guid? excludeRuleId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsPenaltyRuleReferencedAsync(Guid ruleId, CancellationToken cancellationToken = default);

    void AddPenaltyRule(RecoveryRequirementRule rule);

    void RemovePenaltyRule(RecoveryRequirementRule rule);

    // Trust Score rules
    Task<IReadOnlyList<TrustScoreRule>> GetScoreRulesAsync(bool? earningOnly, CancellationToken cancellationToken = default);

    Task<TrustScoreRule?> GetScoreRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingScoreRuleAsync(
        SaveBite.Backend.Models.Enums.TrustScoreRuleType ruleType, DateTime from, Guid excludeRuleId, CancellationToken cancellationToken = default);

    void AddScoreRule(TrustScoreRule rule);

    // Shared

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}