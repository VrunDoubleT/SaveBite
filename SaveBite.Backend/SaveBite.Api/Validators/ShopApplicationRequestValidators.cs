using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

public abstract class ShopApplicationRequestValidatorBase<T> : AbstractValidator<T>
    where T : CreateShopApplicationRequest
{
    protected ShopApplicationRequestValidatorBase()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BusinessLicenseNo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Ward).MaximumLength(100);
        RuleFor(x => x.District).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BankAccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BankAccountHolder).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PayosClientId).MaximumLength(255);
        RuleFor(x => x.PayosApiKey).MaximumLength(500);
        RuleFor(x => x.PayosChecksumKey).MaximumLength(500);
    }
}

public sealed class CreateShopApplicationRequestValidator : ShopApplicationRequestValidatorBase<CreateShopApplicationRequest> { }

public sealed class ResubmitShopApplicationRequestValidator : ShopApplicationRequestValidatorBase<ResubmitShopApplicationRequest> { }