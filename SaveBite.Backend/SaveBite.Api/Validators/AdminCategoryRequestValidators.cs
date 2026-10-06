using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public sealed class GetCategoriesRequestValidator : AbstractValidator<GetCategoriesRequest>
{
    public GetCategoriesRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The category name is required.")
            .MaximumLength(150).WithMessage("The category name must not exceed 150 characters.");
    }
}

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The category name is required.")
            .MaximumLength(150).WithMessage("The category name must not exceed 150 characters.");
    }
}