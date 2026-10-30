using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public sealed class IssueViolationWarningRequestValidator : AbstractValidator<IssueViolationWarningRequest>
{
    public IssueViolationWarningRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Warning reason is required.")
            .MinimumLength(5)
            .WithMessage("Warning reason must contain at least 5 characters.")
            .MaximumLength(1000)
            .WithMessage("Warning reason must not exceed 1000 characters.");

        RuleFor(x => x.RequiredCorrection)
            .NotEmpty()
            .WithMessage("Required correction is required.")
            .MinimumLength(3)
            .WithMessage("Required correction must contain at least 3 characters.")
            .MaximumLength(1000)
            .WithMessage("Required correction must not exceed 1000 characters.");
    }
}