namespace SaveBite.Backend.Caching.Interfaces;

public sealed record RateLimitDecision(bool Allowed, TimeSpan? RetryAfter);

public interface IAuthRedisService
{
    Task<RateLimitDecision> AcquireOtpRequestPermitAsync(
        string purpose,
        string subjectHash,
        TimeSpan cooldown,
        int emailDailyLimit,
        TimeSpan dailyExpiry);

    Task SetJsonAsync<T>(string key, T value, TimeSpan expiry);
    Task<T?> GetJsonAsync<T>(string key);
    Task SetStringAsync(string key, string value, TimeSpan expiry);
    Task<string?> GetStringAsync(string key);
    Task<long> IncrementAsync(string key, TimeSpan expiry);
    Task<TimeSpan?> GetTimeToLiveAsync(string key);
    Task<bool> DeleteIfValueMatchesAsync(string key, string expectedValue);
    Task RemoveAsync(params string[] keys);
}
