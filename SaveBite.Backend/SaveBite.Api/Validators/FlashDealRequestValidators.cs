using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public sealed class FlashDealCursorRequestValidator : AbstractValidator<FlashDealCursorRequest>
{
    public FlashDealCursorRequestValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.RadiusInKm)
            .GreaterThan(0)
            .WithMessage("RadiusInKm must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("RadiusInKm must be between 0 and 100 km.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .WithMessage("Limit must be between 1 and 100.");

        RuleFor(x => x.Keyword)
            .MaximumLength(100)
            .WithMessage("Keyword must not exceed 100 characters.");
    }
}
