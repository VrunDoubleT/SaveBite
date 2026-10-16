using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public sealed class NearbyShopsRequestValidator : AbstractValidator<NearbyShopsRequest>
{
    public NearbyShopsRequestValidator()
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
            .LessThanOrEqualTo(10.0)
            .WithMessage("RadiusInKm must not exceed 10 km.");

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

public sealed class UpdateBusinessInfoRequestValidator : AbstractValidator<UpdateBusinessInfoRequest>
{
    public UpdateBusinessInfoRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Shop name is required.")
            .MinimumLength(2).WithMessage("Shop name must contain at least 2 characters.")
            .MaximumLength(200).WithMessage("Shop name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.Ward).MaximumLength(100).When(x => x.Ward is not null);
        RuleFor(x => x.District).MaximumLength(100).When(x => x.District is not null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
        RuleFor(x => x.Latitude).InclusiveBetween(-90d, 90d).WithMessage("Latitude must be between -90 and 90.");
        RuleFor(x => x.Longitude).InclusiveBetween(-180d, 180d).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.ClosingTime)
            .Must((request, closing) => !request.OpeningTime.HasValue || !closing.HasValue || closing.Value > request.OpeningTime.Value)
            .WithMessage("Closing time must be later than opening time.");
    }
}

public sealed class UpdateShopImagesRequestValidator : AbstractValidator<UpdateShopImagesRequest>
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSize = 5 * 1024 * 1024;

    public UpdateShopImagesRequestValidator()
    {
        RuleFor(x => x.Logo)
            .Must(file => file is null || file.Length > 0).WithMessage("Shop logo must not be empty.")
            .Must(file => file is null || file.Length <= MaxFileSize).WithMessage("Shop logo must not exceed 5 MB.")
            .Must(file => file is null || AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            .WithMessage("Shop logo must be a JPG, PNG or WEBP image.");

        RuleFor(x => x.CoverImage)
            .Must(file => file is null || file.Length <= MaxFileSize).WithMessage("Cover image must not exceed 5 MB.")
            .Must(file => file is null || AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            .WithMessage("Cover image must be a JPG, PNG or WEBP image.");
    }
}
