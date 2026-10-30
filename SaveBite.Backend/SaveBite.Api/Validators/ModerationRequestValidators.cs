using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

// Product Moderation - Remove violating product
public sealed class RemoveViolatingProductRequestValidator : AbstractValidator<RemoveViolatingProductRequest>
{
    public RemoveViolatingProductRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Removal reason is required.")
            .MinimumLength(5)
            .WithMessage("Removal reason must contain at least 5 characters.")
            .MaximumLength(1000)
            .WithMessage("Removal reason must not exceed 1000 characters.");
    }
}

// Feedback Moderation - Hide violating feedback
public sealed class HideViolatingFeedbackRequestValidator : AbstractValidator<HideViolatingFeedbackRequest>
{
    public HideViolatingFeedbackRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Feedback removal reason is required.")
            .MinimumLength(5)
            .WithMessage("Feedback removal reason must contain at least 5 characters.")
            .MaximumLength(1000)
            .WithMessage("Feedback removal reason must not exceed 1000 characters.");
    }
}

// Feedback Moderation - Restore hidden feedback
public sealed class RestoreFeedbackRequestValidator : AbstractValidator<RestoreFeedbackRequest>
{
    public RestoreFeedbackRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(1000)
            .WithMessage("Restore reason must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}