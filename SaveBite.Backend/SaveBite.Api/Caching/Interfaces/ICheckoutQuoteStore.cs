using SaveBite.Backend.Models.DTOs;

namespace SaveBite.Backend.Caching.Interfaces;

// Save and retrieve temporary checkout agreements without a database quote table.
public interface ICheckoutQuoteStore
{
    Task SaveAsync(CheckoutQuote quote, CancellationToken ct = default);
    Task<CheckoutQuote?> GetAsync(Guid userId, Guid quoteId, CancellationToken ct = default);
}
