using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface ITrustConfigService
{
    // UC-129 / UC-131: levels with benefit text and requirements
    Task<IReadOnlyList<AdminTrustLevelResponse>> GetLevelsAsync(CancellationToken cancellationToken = default);

    // UC-130
    Task<AdminTrustLevelResponse> UpdateLevelBenefitsAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelBenefitsRequest request, CancellationToken cancellationToken = default);

    // UC-132
    Task<AdminTrustLevelResponse> UpdateLevelRequirementAsync(
        Guid adminId, Guid levelId, UpdateTrustLevelRequirementRequest request, CancellationToken cancellationToken = default);

    // UC-133
    Task<IReadOnlyList<PenaltyRuleResponse>> GetPenaltyRulesAsync(CancellationToken cancellationToken = default);

    // UC-134
    Task<PenaltyRuleResponse> UpdatePenaltyRuleAsync(
        Guid adminId, Guid ruleId, UpdatePenaltyRuleRequest request, CancellationToken cancellationToken = default);

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
