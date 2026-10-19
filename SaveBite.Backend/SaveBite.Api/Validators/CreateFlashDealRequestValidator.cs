using FluentValidation;
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
        When(x => x.IsImmediate, () =>
        {
            RuleFor(x => x.OrderEndTime)
                .Must(orderEnd => orderEnd >= DateTime.UtcNow.AddMinutes(29)) // 1-minute buffer for request network latency
                .WithMessage("Deal duration must be at least 30 minutes from current time.")
                .Must(orderEnd => orderEnd <= DateTime.UtcNow.AddMinutes(121))
                .WithMessage("Deal duration cannot exceed 120 minutes.")
                .Must(orderEnd => orderEnd.ToLocalTime().Date == DateTime.Now.Date)
                .WithMessage("Flash deal must end within today.");
        });
        // === 2. SCHEDULED SELLING MODE ===
        When(x => !x.IsImmediate, () =>
        {
            RuleFor(x => x.SaleStartTime)
                .NotNull().WithMessage("Sale start time is required when scheduling.")
                .Must(start => start >= DateTime.UtcNow.AddMinutes(14)) // At least 15 minutes in advance
                .WithMessage("Scheduled start time must be at least 15 minutes from now.")
                .Must(start => start <= DateTime.UtcNow.AddMinutes(91)) // Up to 90 minutes in advance
                .WithMessage("Flash deal can only be scheduled to start up to 90 minutes in advance.");
            RuleFor(x => x.OrderEndTime)
                .GreaterThan(x => x.SaleStartTime!.Value)
                .WithMessage("Order end time must be after sale start time.")
                .Must((req, orderEnd) =>
                {
                    if (!req.SaleStartTime.HasValue) return false;
                    var duration = orderEnd - req.SaleStartTime.Value;
                    return duration >= TimeSpan.FromMinutes(30) && duration <= TimeSpan.FromMinutes(120);
                })
                .WithMessage("The duration of a flash deal must be between 30 and 120 minutes.")
                .Must((req, orderEnd) =>
                {
                    if (!req.SaleStartTime.HasValue) return false;
                    return req.SaleStartTime.Value.ToLocalTime().Date == orderEnd.ToLocalTime().Date;
                })
                .WithMessage("Flash deals can only take place within a single calendar day.");
        });
        RuleFor(x => x.ShopClosingTime)
            .GreaterThanOrEqualTo(x => x.OrderEndTime)
            .WithMessage("Shop closing time must be on or after order end time.");

        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.VariantId).NotEmpty().WithMessage("Variant ID is required.");
            v.RuleFor(x => x.DealPrice).GreaterThan(0).WithMessage("Deal price must be greater than 0.");
            v.RuleFor(x => x.TotalQuantity).GreaterThan(0).WithMessage("Total quantity must be greater than 0.");
        });
    }
}

// Validator for updating an existing flash deal.
public sealed class UpdateFlashDealRequestValidator : AbstractValidator<UpdateFlashDealRequest>
{
    public UpdateFlashDealRequestValidator()
    {
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
            v.RuleFor(x => x.DealPrice).GreaterThan(0).WithMessage("Deal price must be greater than 0.");
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