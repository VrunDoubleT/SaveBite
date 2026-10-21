using System.Globalization;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Exceptions;
using StackExchange.Redis;

namespace SaveBite.Backend.Caching.Implementations;

// Allocate six-digit pickup codes independently for each shop and business date.
public sealed class RedisOrderCodeCache : IOrderCodeCache
{
    private readonly IDatabase _redis;
    public RedisOrderCodeCache(IConnectionMultiplexer redis) { _redis = redis.GetDatabase(); }

    // Raise a missing/restored counter to the committed DB floor before allocating; never decrement or reuse numbers.
    private const string Allocate = """
        local current = tonumber(redis.call('GET', KEYS[1]) or '0')
        local floor = tonumber(ARGV[1])
        if current < floor then current = floor end
        if current >= 999999 then return -1 end
        local next = current + 1
        redis.call('SET', KEYS[1], next, 'EXAT', ARGV[2])
        return next
        """;

    // Atomically allocate a daily shop pickup code using Redis and the database counter floor.
    public async Task<string> NextAsync(Guid shopId, DateOnly date, int persistedMaximum, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var expiry = new DateTimeOffset(date.AddDays(3).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUnixTimeSeconds();
        try
        {
            var result = await _redis.ScriptEvaluateAsync(Allocate, [RedisKeys.OrderCode(shopId, date)], [persistedMaximum, expiry]).WaitAsync(ct);
            var number = (long)result;
            if (number < 1) throw AppException.Conflict("This shop has reached its daily pickup-code limit.");
            return number.ToString("D6", CultureInfo.InvariantCulture);
        }
        catch (RedisException)
        { throw AppException.ServiceUnavailable("Pickup code allocation is temporarily unavailable. Please try again."); }
    }
}
