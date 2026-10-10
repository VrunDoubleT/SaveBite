using System.Text.Json;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Exceptions;
using StackExchange.Redis;

namespace SaveBite.Backend.Caching.Implementations;

public sealed class AuthRedisService : IAuthRedisService
{
    private const string CompareAndDeleteScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then " +
        "return redis.call('del', KEYS[1]) else return 0 end";

    private readonly IDatabase _database;
    private readonly ILogger<AuthRedisService> _logger;

    public AuthRedisService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<AuthRedisService> logger)
    {
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task<RateLimitDecision> AcquireOtpRequestPermitAsync(
        string purpose,
        string subjectHash,
        TimeSpan cooldown,
        int emailDailyLimit,
        TimeSpan dailyExpiry)
    {
        try
        {
            var cooldownKey = RedisKeys.AuthOtpCooldown(purpose, subjectHash);

            // SET NX makes the cooldown check atomic across all API instances.
            var acquired = await _database.StringSetAsync(
                cooldownKey,
                "1",
                cooldown,
                When.NotExists);

            if (!acquired)
                return new RateLimitDecision(
                    false,
                    await _database.KeyTimeToLiveAsync(cooldownKey));

            var emailKey = RedisKeys.AuthOtpDailyEmail(purpose, subjectHash);

            // Count requests per email and purpose until the next UTC day.
            var emailCount = await IncrementWithExpiryAsync(emailKey, dailyExpiry);
            if (emailCount > emailDailyLimit)
            {
                await _database.KeyDeleteAsync(cooldownKey);
                return new RateLimitDecision(
                    false,
                    await _database.KeyTimeToLiveAsync(emailKey));
            }

            return new RateLimitDecision(true, null);
        }
        catch (RedisException exception)
        {
            throw RedisUnavailable(exception);
        }
    }

    public async Task SetJsonAsync<T>(string key, T value, TimeSpan expiry)
    {
        try
        {
            await _database.StringSetAsync(
                key,
                JsonSerializer.Serialize(value),
                expiry);
        }
        catch (RedisException exception)
        {
            throw RedisUnavailable(exception);
        }
    }

    public async Task<T?> GetJsonAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            return value.IsNullOrEmpty
                ? default
                : JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (RedisException exception)
        {
            throw RedisUnavailable(exception);
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan expiry)
    {
        try
        {
            await _database.StringSetAsync(key, value, expiry);
        }
        catch (RedisException exception)
        {
            throw RedisUnavailable(exception);
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (RedisException exception)
        {
            throw RedisUnavailable(exception);
        }
    }

    public async Task<long> IncrementAsync(string key, TimeSpan expiry)
    {
        try
        {
            return await IncrementWithExpiryAsync(key, expiry);
        }
        catch (RedisException exception)
        {
            throw RedisUnavailable(exception);
        }
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
    {
        try
        {
            return await _database.KeyTimeToLiveAsync(key);
        }
        catch (RedisException exception)
        {
            throw RedisUnavailable(exception);
        }
    }

    public async Task<bool> DeleteIfValueMatchesAsync(
        string key,
        string expectedValue)
    {
        try
        {
            // The Lua script consumes an OTP only when its value is unchanged,.
            // Preventing two concurrent verification requests from succeeding.
            var result = await _database.ScriptEvaluateAsync(
                CompareAndDeleteScript,
                new RedisKey[] { key },
                new RedisValue[] { expectedValue });
            return (long)result == 1;
        }
        catch (RedisException exception)
        {
            throw RedisUnavailable(exception);
        }
    }

    public async Task RemoveAsync(params string[] keys)
    {
        if (keys.Length == 0) return;

        try
        {
            await _database.KeyDeleteAsync(
                keys.Select(key => (RedisKey)key).ToArray());
        }
        catch (RedisException exception)
        {
            throw RedisUnavailable(exception);
        }
    }

    private async Task<long> IncrementWithExpiryAsync(
        RedisKey key,
        TimeSpan expiry)
    {
        var count = await _database.StringIncrementAsync(key);

        // Set the expiry only on first creation so repeated attempts cannot extend.
        // The configured rate-limit window indefinitely.
        if (count == 1)
            await _database.KeyExpireAsync(key, expiry);
        return count;
    }

    private AppException RedisUnavailable(RedisException exception)
    {
        _logger.LogError(exception, "Redis is unavailable for an authentication operation.");
        return AppException.ServiceUnavailable(
            "Authentication is temporarily unavailable. Please try again later.");
    }
}
