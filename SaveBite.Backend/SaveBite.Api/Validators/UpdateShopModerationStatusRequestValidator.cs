using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public sealed class UpdateShopModerationStatusRequestValidator : AbstractValidator<UpdateShopModerationStatusRequest>
{
    public UpdateShopModerationStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Shop status is required.")
            .Must(x => new[] { "Active", "Paused", "Suspended" }.Contains(x?.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be Active, Paused or Suspended.");

        When(x => x.Status?.Trim().Equals("Paused", StringComparison.OrdinalIgnoreCase) == true || x.Status?.Trim().Equals("Suspended", StringComparison.OrdinalIgnoreCase) == true, () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Reason is required.")
                .Must(x => !string.IsNullOrWhiteSpace(x) && x.Trim().Length is >= 5 and <= 1000)
                .WithMessage("Reason must be between 5 and 1000 characters.");
        });
    }
}
