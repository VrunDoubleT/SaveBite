using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

// Validator for creating a new flash deal.
public sealed class CreateFlashDealRequestValidator : AbstractValidator<CreateFlashDealRequest>
{
    public CreateFlashDealRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.SaleStartTime)
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Sale start time cannot be in the past.");

        RuleFor(x => x.OrderEndTime)
            .GreaterThan(x => x.SaleStartTime)
            .WithMessage("Order end time must be after sale start time.");

        RuleFor(x => x.ShopClosingTime)
            .GreaterThanOrEqualTo(x => x.OrderEndTime)
            .WithMessage("Shop closing time must be on or after order end time.");

        RuleFor(x => x.Variants)
            .NotEmpty().WithMessage("At least one variant must be included in the flash deal.");

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