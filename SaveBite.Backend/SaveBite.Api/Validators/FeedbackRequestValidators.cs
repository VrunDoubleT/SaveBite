using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public sealed class CreateFeedbackRequestValidator : AbstractValidator<FeedbackRequests.CreateFeedbackRequest>
{
    public CreateFeedbackRequestValidator()
    {
        RuleFor(x => x.OrderItemId)
            .NotEmpty().WithMessage("OrderItemId is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(2000).WithMessage("Comment cannot exceed 2000 characters.");

        RuleFor(x => x.Images)
            .Must(imgs => imgs == null || imgs.Count <= 5)
            .WithMessage("A feedback cannot contain more than 5 images.");
    }
}

public sealed class UpdateFeedbackRequestValidator : AbstractValidator<FeedbackRequests.UpdateFeedbackRequest>
{
    public UpdateFeedbackRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(2000).WithMessage("Comment cannot exceed 2000 characters.");

        RuleFor(x => x.Images)
            .Must(imgs => imgs == null || imgs.Count <= 5)
            .WithMessage("A feedback cannot contain more than 5 images.");
    }
}

public sealed class CreateFeedbackReplyRequestValidator : AbstractValidator<FeedbackRequests.CreateFeedbackReplyRequest>
{
    public CreateFeedbackReplyRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Reply content cannot be empty.")
            .MaximumLength(2000).WithMessage("Reply content cannot exceed 2000 characters.");
    }
}

public sealed class UpdateFeedbackReplyRequestValidator : AbstractValidator<FeedbackRequests.UpdateFeedbackReplyRequest>
{
    public UpdateFeedbackReplyRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Reply content cannot be empty.")
            .MaximumLength(2000).WithMessage("Reply content cannot exceed 2000 characters.");
    }
}
