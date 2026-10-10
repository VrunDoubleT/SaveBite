using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

// USER PROFILE 
public sealed class UpdateUserProfileRequestValidators : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidators()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Phone)
            .Matches(@"^\d+$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone number is invalid.");

        RuleFor(x => x.Phone)
            .Must(phone => phone!.Length == 9 || phone.Length == 10)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone number must contain 9 or 10 digits.");
    }
}

// ADDRESS MANAGEMENT
public sealed class CreateAddressRequestValidator : AbstractValidator<UserAddressRequests.CreateAddressRequest>
{
    public CreateAddressRequestValidator()
    {
        RuleFor(x => x.Label)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Label));

        RuleFor(x => x.AddressLine)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Ward)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Ward));

        RuleFor(x => x.District)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.District));

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180.");
    }
}

public sealed class UpdateAddressRequestValidator : AbstractValidator<UserAddressRequests.UpdateAddressRequest>
{
    public UpdateAddressRequestValidator()
    {
        RuleFor(x => x.Label)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Label));

        RuleFor(x => x.AddressLine)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Ward)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Ward));

        RuleFor(x => x.District)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.District));

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180.");
    }
}
