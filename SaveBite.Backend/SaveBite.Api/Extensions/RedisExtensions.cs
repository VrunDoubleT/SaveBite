using SaveBite.Backend.Caching.Implementations;
using SaveBite.Backend.Caching.Interfaces;
using StackExchange.Redis;

namespace SaveBite.Backend.Extensions;

public static class RedisExtensions
{
    public static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString =
            configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        services.AddSingleton<IAuthRedisService, AuthRedisService>();
        services.AddSingleton<IFlashDealRedisService, FlashDealRedisService>();

        return services;
    }
}
