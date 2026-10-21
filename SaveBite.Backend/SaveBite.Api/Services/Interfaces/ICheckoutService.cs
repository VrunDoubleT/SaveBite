using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

// Provide checkout context, temporary quotes and idempotent order placement.
public interface ICheckoutService
{
    Task<CheckoutContextResponse> ContextAsync(Guid userId, Guid shopId, CancellationToken ct = default);
    Task<CheckoutQuoteResponse> QuoteAsync(Guid userId, CheckoutQuoteRequest request, CancellationToken ct = default);
    Task<CheckoutOrderResponse> PlaceAsync(Guid userId, PlaceCheckoutOrderRequest request, CancellationToken ct = default);
}
