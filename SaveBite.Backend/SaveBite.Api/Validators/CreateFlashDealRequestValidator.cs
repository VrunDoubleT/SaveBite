using SaveBite.Backend.Configurations;
using FluentValidation;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

// Validator for creating a new flash deal.
public sealed class CreateFlashDealRequestValidator : AbstractValidator<CreateFlashDealRequest>
{
    public CreateFlashDealRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.Variants).NotEmpty().WithMessage("At least one variant must be included.");
        
        // === 1. DIRECT SALE MODE ===
        RuleFor(x => x.DurationMinutes).InclusiveBetween(TimingConfiguration.FlashDeals.MinimumDurationMinutes, TimingConfiguration.FlashDeals.MaximumDurationMinutes)
            .When(x => x.DurationMinutes.HasValue)
            .WithMessage($"The duration of a flash deal must be between {TimingConfiguration.FlashDeals.MinimumDurationMinutes} and {TimingConfiguration.FlashDeals.MaximumDurationMinutes} minutes.");
        When(x => x.IsImmediate && !x.DurationMinutes.HasValue, () =>
        {
            RuleFor(x => x.OrderEndTime)
                .Must(orderEnd => orderEnd >= DateTime.UtcNow.AddMinutes(TimingConfiguration.FlashDeals.MinimumDurationMinutes - TimingConfiguration.FlashDeals.LegacyRequestToleranceMinutes)) // 1-minute buffer for request network latency
                .WithMessage($"Deal duration must be at least {TimingConfiguration.FlashDeals.MinimumDurationMinutes} minutes from current time.")
                .Must(orderEnd => orderEnd <= DateTime.UtcNow.AddMinutes(TimingConfiguration.FlashDeals.MaximumDurationMinutes + TimingConfiguration.FlashDeals.LegacyRequestToleranceMinutes))
                .WithMessage($"Deal duration cannot exceed {TimingConfiguration.FlashDeals.MaximumDurationMinutes} minutes.");
        });
        // === 2. SCHEDULED SELLING MODE ===
        When(x => !x.IsImmediate, () =>
        {
            RuleFor(x => x.SaleStartTime)
                .NotNull().WithMessage("Sale start time is required when scheduling.")
                .Must(start => start >= DateTime.UtcNow.AddMinutes(TimingConfiguration.FlashDeals.MinimumScheduleAheadMinutes - TimingConfiguration.FlashDeals.LegacyRequestToleranceMinutes)) // At least 15 minutes in advance
                .WithMessage($"Scheduled start time must be at least {TimingConfiguration.FlashDeals.MinimumScheduleAheadMinutes} minutes from now.")
                .Must(start => start <= DateTime.UtcNow.AddMinutes(TimingConfiguration.FlashDeals.MaximumScheduleAheadMinutes + TimingConfiguration.FlashDeals.LegacyRequestToleranceMinutes)) // Up to 90 minutes in advance
                .WithMessage($"Flash deal can only be scheduled to start up to {TimingConfiguration.FlashDeals.MaximumScheduleAheadMinutes} minutes in advance.");
            RuleFor(x => x.OrderEndTime)
                .GreaterThan(x => x.SaleStartTime!.Value)
                .WithMessage("Order end time must be after sale start time.")
                .Must((req, orderEnd) =>
                {
                    if (!req.SaleStartTime.HasValue) return false;
                    var duration = orderEnd - req.SaleStartTime.Value;
                    return duration >= TimeSpan.FromMinutes(TimingConfiguration.FlashDeals.MinimumDurationMinutes) && duration <= TimeSpan.FromMinutes(TimingConfiguration.FlashDeals.MaximumDurationMinutes);
                })
                .WithMessage($"The duration of a flash deal must be between {TimingConfiguration.FlashDeals.MinimumDurationMinutes} and {TimingConfiguration.FlashDeals.MaximumDurationMinutes} minutes.");
        });
        RuleFor(x => x.ShopClosingTime)
            .GreaterThanOrEqualTo(x => x.OrderEndTime)
            .WithMessage("Shop closing time must be on or after order end time.");

        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.VariantId).NotEmpty().WithMessage("Variant ID is required.");
            v.RuleFor(x => x.DealPrice)
                .GreaterThanOrEqualTo(MoneyLimits.MinAmountVnd).WithMessage($"Deal price must be at least {MoneyLimits.MinAmountVnd:N0} VND.")
                .Must(p => p % 1000 == 0).WithMessage("Deal price must be a multiple of 1,000 VND.");
            v.RuleFor(x => x.TotalQuantity).GreaterThan(0).WithMessage("Total quantity must be greater than 0.");
        });
    }
}

// Validator for updating an existing flash deal.
public sealed class UpdateFlashDealRequestValidator : AbstractValidator<UpdateFlashDealRequest>
{
    public UpdateFlashDealRequestValidator()
    {
        RuleFor(x => x.DurationMinutes).InclusiveBetween(TimingConfiguration.FlashDeals.MinimumDurationMinutes, TimingConfiguration.FlashDeals.MaximumDurationMinutes)
            .When(x => x.DurationMinutes.HasValue)
            .WithMessage($"The duration of a flash deal must be between {TimingConfiguration.FlashDeals.MinimumDurationMinutes} and {TimingConfiguration.FlashDeals.MaximumDurationMinutes} minutes.");
        RuleFor(x => x.SaleStartTime)
            .NotEmpty().WithMessage("Sale start time is required.");

        RuleFor(x => x.OrderEndTime)
            .GreaterThan(x => x.SaleStartTime)
            .WithMessage("Order end time must be after sale start time.");

        RuleFor(x => x.ShopClosingTime)
            .GreaterThanOrEqualTo(x => x.OrderEndTime)
            .WithMessage("Shop closing time must be on or after order end time.");

        RuleFor(x => x.Variants)
            .NotEmpty().WithMessage("At least one variant must be included.");

        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.VariantId).NotEmpty().WithMessage("Variant ID is required.");
            v.RuleFor(x => x.DealPrice)
                .GreaterThanOrEqualTo(MoneyLimits.MinAmountVnd).WithMessage($"Deal price must be at least {MoneyLimits.MinAmountVnd:N0} VND.")
                .Must(p => p % 1000 == 0).WithMessage("Deal price must be a multiple of 1,000 VND.");
            v.RuleFor(x => x.TotalQuantity).GreaterThan(0).WithMessage("Total quantity must be greater than 0.");
        });
    }
}

// Validator for shop flash deal query filters.
public sealed class ShopFlashDealQueryRequestValidator : AbstractValidator<ShopFlashDealQueryRequest>
{
    public ShopFlashDealQueryRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("ToDate must be greater than or equal to FromDate.");
    }
}
