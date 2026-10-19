namespace SaveBite.Backend.Models.Requests;

// Carry the customer's GPS coordinates, accuracy and capture time.
public sealed record CheckoutLocation(double Latitude, double Longitude, double AccuracyMeters, DateTime CapturedAt);
// Select one shop and provide GPS data for checkout calculations.
public sealed record CheckoutQuoteRequest(Guid ShopId, CheckoutLocation Location);
// Submit an agreed quote, payment method and idempotent request ID.
public sealed record PlaceCheckoutOrderRequest(Guid QuoteId, string PaymentMethod, Guid RequestId);
