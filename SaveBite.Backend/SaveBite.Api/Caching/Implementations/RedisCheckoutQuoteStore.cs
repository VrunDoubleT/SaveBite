using System.Text.Json;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Caching.Interfaces;
using StackExchange.Redis;

namespace SaveBite.Backend.Caching.Implementations;

// Store customer-scoped checkout quotes in Redis with bounded expiry.
public sealed class RedisCheckoutQuoteStore : ICheckoutQuoteStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeProvider _clock;

    public RedisCheckoutQuoteStore(IConnectionMultiplexer redis, TimeProvider clock)
    {
        _redis = redis;
        _clock = clock;
    }

    // Cache the agreed checkout calculations under a customer-scoped quote key until expiry.
    public async Task SaveAsync(CheckoutQuote quote, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var ttl = quote.ExpiresAt - _clock.GetUtcNow().UtcDateTime;
        if (ttl <= TimeSpan.Zero) throw AppException.Conflict("Your checkout quote expired. Refresh GPS and try again.");
        try
        {
            await _redis.GetDatabase().StringSetAsync(RedisKeys.CheckoutQuote(quote.UserId, quote.Id), JsonSerializer.Serialize(quote), ttl);
        }
        catch (RedisException)
        {
            throw AppException.ServiceUnavailable("Checkout is temporarily unavailable. Please try again.");
        }
    }

    // Read a customer's quote and reject missing or expired cached data.
    public async Task<CheckoutQuote?> GetAsync(Guid userId, Guid quoteId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(RedisKeys.CheckoutQuote(userId, quoteId));
            if (value.IsNullOrEmpty) return null;
            var quote = JsonSerializer.Deserialize<CheckoutQuote>((string)value!);
            return quote != null && quote.UserId == userId && quote.Id == quoteId
                && quote.ExpiresAt > _clock.GetUtcNow().UtcDateTime ? quote : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (RedisException)
        {
            throw AppException.ServiceUnavailable("Checkout is temporarily unavailable. Please try again.");
        }
    }
}
