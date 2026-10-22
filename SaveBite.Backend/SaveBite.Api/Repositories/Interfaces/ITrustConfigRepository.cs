using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

/// <summary>Admin-side access to Trust Score configuration (UC-129 .. UC-138).</summary>
public interface ITrustConfigRepository
{
    // Levels
    Task<IReadOnlyList<TrustLevel>> GetLevelsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetUserCountsByLevelAsync(CancellationToken cancellationToken = default);

    /// <summary>Tracked level (with requirement and deposit policy) for update.</summary>
    Task<TrustLevel?> GetLevelForUpdateAsync(Guid levelId, CancellationToken cancellationToken = default);

    /// <summary>Tracked active levels (with requirement) ordered by rank, used to validate thresholds.</summary>
    Task<IReadOnlyList<TrustLevel>> GetActiveLevelsForValidationAsync(CancellationToken cancellationToken = default);

    void AddDepositPolicy(TrustLevelDepositPolicy policy);

    void AddDepositRanges(IEnumerable<TrustLevelDepositRange> ranges);

    void RemoveDepositRanges(IEnumerable<TrustLevelDepositRange> ranges);

    // Penalty rules
    Task<IReadOnlyList<RecoveryRequirementRule>> GetPenaltyRulesAsync(CancellationToken cancellationToken = default);

    Task<RecoveryRequirementRule?> GetPenaltyRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingPenaltyRuleAsync(
        int minimumOccurrence, DateTime from, Guid excludeRuleId, CancellationToken cancellationToken = default);

    void AddPenaltyRule(RecoveryRequirementRule rule);

    // Trust Score rules
    Task<IReadOnlyList<TrustScoreRule>> GetScoreRulesAsync(bool? earningOnly, CancellationToken cancellationToken = default);

    Task<TrustScoreRule?> GetScoreRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingScoreRuleAsync(
        SaveBite.Backend.Models.Enums.TrustScoreRuleType ruleType, DateTime from, Guid excludeRuleId, CancellationToken cancellationToken = default);

    void AddScoreRule(TrustScoreRule rule);

    // Shared
    void AddAuditLog(AuditLog log);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}