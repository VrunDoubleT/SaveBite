using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

// numeric(12,2) columns: at most 10 integer digits and 2 decimals.
internal static class TrustValidationRules
{
    public const int MaxCount = 1_000_000;
    public const int MaxScoreDelta = 1_000;
}

public sealed class GetTrustScoreHistoryRequestValidator : AbstractValidator<GetTrustScoreHistoryRequest>
{
    public GetTrustScoreHistoryRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.EventType).IsInEnum().When(x => x.EventType.HasValue);
    }
}

public sealed class GetPenaltiesRequestValidator : AbstractValidator<GetPenaltiesRequest>
{
    public GetPenaltiesRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}

public sealed class UpdateTrustLevelBenefitsRequestValidator : AbstractValidator<UpdateTrustLevelBenefitsRequest>
{
    public UpdateTrustLevelBenefitsRequestValidator()
    {
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class UpdateTrustLevelRequirementRequestValidator : AbstractValidator<UpdateTrustLevelRequirementRequest>
{
    public UpdateTrustLevelRequirementRequestValidator()
    {
        RuleFor(x => x.MinScore).InclusiveBetween(0, TrustValidationRules.MaxCount);
        RuleFor(x => x.MinCompletedOrders).InclusiveBetween(0, TrustValidationRules.MaxCount);
        RuleFor(x => x.MinTotalOrderValue)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(12, 2, false);
    }
}

public sealed class UpdatePenaltyRuleRequestValidator : AbstractValidator<UpdatePenaltyRuleRequest>
{
    public UpdatePenaltyRuleRequestValidator()
    {
        RuleFor(x => x.MinimumOccurrence).InclusiveBetween(1, 100);
        RuleFor(x => x.ScoreDeduction).InclusiveBetween(1, TrustValidationRules.MaxScoreDelta);
        RuleFor(x => x.RequiredOnlineOrders).InclusiveBetween(0, TrustValidationRules.MaxCount);
        RuleFor(x => x.RequiredOnlineOrderValue)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(12, 2, false);
    }
}

public sealed class UpdateTrustScoreRuleRequestValidator : AbstractValidator<UpdateTrustScoreRuleRequest>
{
    public UpdateTrustScoreRuleRequestValidator()
    {
        RuleFor(x => x.ScoreDelta)
            .NotEqual(0)
            .InclusiveBetween(-TrustValidationRules.MaxScoreDelta, TrustValidationRules.MaxScoreDelta);
    }
}
