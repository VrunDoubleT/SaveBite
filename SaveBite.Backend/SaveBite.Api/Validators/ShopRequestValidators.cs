using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public sealed class NearbyShopsRequestValidator : AbstractValidator<NearbyShopsRequest>
{
    public NearbyShopsRequestValidator()
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

        RuleFor(x => x.Keyword)
            .MaximumLength(100)
            .WithMessage("Keyword must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Keyword));
    }
}

public sealed class StoreReviewsQueryRequestValidator : AbstractValidator<StoreReviewsQueryRequest>
{
    public StoreReviewsQueryRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("PageSize must be between 1 and 50.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .When(x => x.Rating.HasValue)
            .WithMessage("Rating must be between 1 and 5 stars.");
    }
}
