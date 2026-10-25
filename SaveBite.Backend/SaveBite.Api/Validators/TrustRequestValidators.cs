using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

// numeric(12,2) columns: at most 10 integer digits and 2 decimals.
internal static class TrustValidationRules
{
    public const int MaxCount = 1_000_000;
    public const int MaxScoreDelta = 1_000;
    public const int MaxDepositRanges = 20;
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

public sealed class TrustLevelDepositRangeRequestValidator : AbstractValidator<TrustLevelDepositRangeRequest>
{
    public TrustLevelDepositRangeRequestValidator()
    {
        RuleFor(x => x.FromOrderAmount)
            .GreaterThan(0)
            .WithMessage("Range start amount must be greater than 0.")
            .PrecisionScale(12, 2, false)
            .Must(IsWholeVnd)
            .WithMessage("Range start amount must be a whole VND amount.");

        RuleFor(x => x.ToOrderAmount)
            .GreaterThan(x => x.FromOrderAmount)
            .WithMessage("Range end amount must be greater than range start amount.")
            .PrecisionScale(12, 2, false)
            .Must(IsWholeVnd)
            .WithMessage("Range end amount must be a whole VND amount.");

        RuleFor(x => x.DepositAmount)
            .GreaterThan(0)
            .WithMessage("Deposit amount must be greater than 0.")
            .LessThanOrEqualTo(x => x.FromOrderAmount)
            .WithMessage("Deposit amount cannot exceed the range start amount.")
            .PrecisionScale(12, 2, false)
            .Must(IsWholeVnd)
            .WithMessage("Deposit amount must be a whole VND amount.");
    }

    private static bool IsWholeVnd(decimal value) => decimal.Truncate(value) == value;
}

public sealed class UpdateTrustLevelBenefitsRequestValidator : AbstractValidator<UpdateTrustLevelBenefitsRequest>
{
    public UpdateTrustLevelBenefitsRequestValidator()
    {
        RuleFor(x => x.Description).MaximumLength(2000);

        When(x => x.RequireFullOnlinePayment, () =>
        {
            RuleFor(x => x.DepositFreeOrderAmount)
                .Null()
                .WithMessage("Deposit-free order amount must be empty when full online payment is required.");

            RuleFor(x => x.MaxDepositOrderAmount)
                .Null()
                .WithMessage("Maximum deposit order amount must be empty when full online payment is required.");

            RuleFor(x => x.Ranges)
                .Must(ranges => ranges == null || ranges.Count == 0)
                .WithMessage("Deposit ranges are not allowed when full online payment is required.");
        });

        When(x => !x.RequireFullOnlinePayment, () =>
        {
            RuleFor(x => x.DepositFreeOrderAmount)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("Deposit-free order amount is required when Pay at shop is enabled.")
                .Must(v => v!.Value > 0)
                .WithMessage("Deposit-free order amount must be greater than 0.")
                .Must(v => decimal.Truncate(v!.Value) == v.Value)
                .WithMessage("Deposit-free order amount must be a whole VND amount.")
                .PrecisionScale(12, 2, false);

            RuleFor(x => x.MaxDepositOrderAmount)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("Maximum deposit order amount is required when Pay at shop is enabled.")
                .Must((req, max) => !req.DepositFreeOrderAmount.HasValue || max!.Value > req.DepositFreeOrderAmount.Value)
                .WithMessage("Maximum deposit order amount must be greater than the deposit-free order amount.")
                .Must(v => decimal.Truncate(v!.Value) == v.Value)
                .WithMessage("Maximum deposit order amount must be a whole VND amount.")
                .PrecisionScale(12, 2, false);

            RuleFor(x => x.Ranges)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .Must(ranges => ranges.Count is >= 1 and <= TrustValidationRules.MaxDepositRanges)
                .WithMessage($"Provide between 1 and {TrustValidationRules.MaxDepositRanges} deposit ranges.");

            RuleForEach(x => x.Ranges).SetValidator(new TrustLevelDepositRangeRequestValidator());
        });
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