using FluentValidation;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Validators;

// Validate shop identity and customer GPS input before quote calculation.
public sealed class CheckoutQuoteRequestValidator : AbstractValidator<CheckoutQuoteRequest>
{
    public CheckoutQuoteRequestValidator()
    {
        RuleFor(x => x.ShopId).NotEmpty();
        RuleFor(x => x.Location).NotNull();
        When(x => x.Location != null, () =>
        {
            RuleFor(x => x.Location.Latitude).InclusiveBetween(-90, 90).Must(double.IsFinite);
            RuleFor(x => x.Location.Longitude).InclusiveBetween(-180, 180).Must(double.IsFinite);
            RuleFor(x => x.Location.AccuracyMeters).InclusiveBetween(0, 1000).Must(double.IsFinite);
            RuleFor(x => x.Location.CapturedAt).NotEmpty();
        });
    }
}
// Validate quote identity, payment method and idempotency key before order placement.
public sealed class PlaceCheckoutOrderRequestValidator : AbstractValidator<PlaceCheckoutOrderRequest>
{
    public PlaceCheckoutOrderRequestValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.PaymentMethod).Must(x => x is "OnlinePayment" or "PayAtShop");
    }
}
