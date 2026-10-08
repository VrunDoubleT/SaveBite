using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public sealed class NearbyFlashDealsRequestValidator : AbstractValidator<NearbyFlashDealsRequest>
{
    public NearbyFlashDealsRequestValidator()
    {
        RuleFor(x => x.Latitude)
            .NotNull()
            .WithMessage("Latitude is required.")
            .InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .NotNull()
            .WithMessage("Longitude is required.")
            .InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.RadiusInKm)
            .GreaterThan(0)
            .WithMessage("RadiusInKm must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("RadiusInKm must be between 0 and 100 km.");
    }
}
