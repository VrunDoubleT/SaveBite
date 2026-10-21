using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

// Validate the selected deal variant and positive add-to-cart quantity.
public sealed class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.FlashDealVariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

// Require a positive replacement cart quantity.
public sealed class UpdateCartItemQuantityRequestValidator : AbstractValidator<UpdateCartItemQuantityRequest>
{
    public UpdateCartItemQuantityRequestValidator() => RuleFor(x => x.Quantity).GreaterThan(0);
}
