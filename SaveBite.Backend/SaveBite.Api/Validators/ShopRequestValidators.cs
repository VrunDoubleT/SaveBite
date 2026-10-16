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

public sealed class UpdateShopPaymentConfigRequestValidator : AbstractValidator<UpdateShopPaymentConfigRequest>
{
    public UpdateShopPaymentConfigRequestValidator()
    {
        RuleFor(x => x.BankName).MaximumLength(100).When(x => x.BankName is not null);
        RuleFor(x => x.BankAccountNumber).MaximumLength(50).When(x => x.BankAccountNumber is not null);
        RuleFor(x => x.BankAccountHolder).MaximumLength(150).When(x => x.BankAccountHolder is not null);
        RuleFor(x => x.PayosClientId).NotEmpty().MaximumLength(255);
        RuleFor(x => x.PayosApiKey).MaximumLength(500);
        RuleFor(x => x.PayosChecksumKey).MaximumLength(500);
        RuleFor(x => x).Must(x =>
            (string.IsNullOrWhiteSpace(x.BankName) &&
             string.IsNullOrWhiteSpace(x.BankAccountNumber) &&
             string.IsNullOrWhiteSpace(x.BankAccountHolder)) ||
            (!string.IsNullOrWhiteSpace(x.BankName) &&
             !string.IsNullOrWhiteSpace(x.BankAccountNumber) &&
             !string.IsNullOrWhiteSpace(x.BankAccountHolder)))
            .WithMessage("Bank name, account number and account holder must all be provided together.");
    }
}
