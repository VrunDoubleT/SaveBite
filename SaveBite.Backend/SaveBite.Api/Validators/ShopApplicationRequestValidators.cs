using FluentValidation;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

// Validate shop application requests.
public sealed class CreateShopApplicationRequestValidator : AbstractValidator<CreateShopApplicationRequest>
{
    public CreateShopApplicationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);

        RuleFor(x => x).Custom((x, context) =>
        {
            if (!x.OpeningTime.HasValue || !x.ClosingTime.HasValue) return;

            if (x.OpeningTime >= x.ClosingTime)
            {
                context.AddFailure(nameof(x.OpeningTime), "Opening time must be earlier than closing time.");
                context.AddFailure(nameof(x.ClosingTime), "Closing time must be later than opening time.");
            }
        });

        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankAccountNumber).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankAccountHolder).NotEmpty().MaximumLength(200);
    }
}

// Validate shop application resubmissions.
public sealed class ResubmitShopApplicationRequestValidator : AbstractValidator<ResubmitShopApplicationRequest>
{
    public ResubmitShopApplicationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);

        RuleFor(x => x).Custom((x, context) =>
        {
            if (!x.OpeningTime.HasValue || !x.ClosingTime.HasValue) return;

            if (x.OpeningTime >= x.ClosingTime)
            {
                context.AddFailure(nameof(x.OpeningTime), "Opening time must be earlier than closing time.");
                context.AddFailure(nameof(x.ClosingTime), "Closing time must be later than opening time.");
            }
        });

        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankAccountNumber).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankAccountHolder).NotEmpty().MaximumLength(200);
    }
}

// Validate shop application documents.
public sealed record ShopApplicationDocumentsValidationRequest(IReadOnlyList<IFormFile> Files, IReadOnlyList<string> Types);

public sealed class ShopApplicationDocumentsValidator : AbstractValidator<ShopApplicationDocumentsValidationRequest>
{
    public ShopApplicationDocumentsValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Files.Count == x.Types.Count)
            .WithMessage("Each uploaded document must have a document type.");

        RuleFor(x => x.Files)
            .Must(files => files.Count <= 10)
            .WithMessage("You can upload up to 10 documents.");
    }
}
