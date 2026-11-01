using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface ITrustConfigService
{
    // UC-129 / UC-131 / UC-152: levels with benefit text, requirements, and customer counts
    Task<IReadOnlyList<AdminTrustLevelResponse>> GetLevelsAsync(CancellationToken cancellationToken = default);

    // UC-152: level version history
    Task<IReadOnlyList<TrustLevelHistoryItemResponse>> GetLevelHistoryAsync(
        Guid levelId, CancellationToken cancellationToken = default);

    // UC-153: add new level at max(rank) + 1
    Task<AdminTrustLevelResponse> CreateLevelAsync(
        Guid adminId, CreateTrustLevelRequest request, CancellationToken cancellationToken = default);

    // UC-154: rename level
    Task<AdminTrustLevelResponse> UpdateLevelInfoAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelInfoRequest request, CancellationToken cancellationToken = default);

    // UC-155: deactivate or reactivate level
    Task<AdminTrustLevelResponse> UpdateLevelStatusAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelStatusRequest request, CancellationToken cancellationToken = default);

    // UC-130
    Task<AdminTrustLevelResponse> UpdateLevelBenefitsAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelBenefitsRequest request, CancellationToken cancellationToken = default);

    // UC-132
    Task<AdminTrustLevelResponse> UpdateLevelRequirementAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelRequirementRequest request, CancellationToken cancellationToken = default);

    // UC-133
    Task<IReadOnlyList<PenaltyRuleResponse>> GetPenaltyRulesAsync(CancellationToken cancellationToken = default);

    // UC-156: create new penalty rule
    Task<PenaltyRuleResponse> CreatePenaltyRuleAsync(
        Guid adminId, CreatePenaltyRuleRequest request, CancellationToken cancellationToken = default);

    // UC-134: update penalty rule
    Task<PenaltyRuleResponse> UpdatePenaltyRuleAsync(
        Guid adminId, Guid ruleId, UpdatePenaltyRuleRequest request, CancellationToken cancellationToken = default);

    // Delete future scheduled penalty rule
    Task DeletePenaltyRuleAsync(
        Guid adminId, Guid ruleId, CancellationToken cancellationToken = default);

    // UC-135: all Trust Score rules (earning and penalty)
    Task<IReadOnlyList<TrustScoreRuleResponse>> GetScoreRulesAsync(CancellationToken cancellationToken = default);

    // UC-136
    Task<TrustScoreRuleResponse> UpdateScoreRuleAsync(
        Guid adminId, Guid ruleId, UpdateTrustScoreRuleRequest request, CancellationToken cancellationToken = default);

    // UC-137: earning rules only (ScoreDelta > 0)
    Task<IReadOnlyList<TrustScoreRuleResponse>> GetEarningRulesAsync(CancellationToken cancellationToken = default);

    // UC-138
    Task<TrustScoreRuleResponse> UpdateEarningRuleAsync(
        Guid adminId, Guid ruleId, UpdateTrustScoreRuleRequest request, CancellationToken cancellationToken = default);
}