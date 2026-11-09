using System.Globalization;
using System.Text.Json;
using SaveBite.Backend.Caching;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;
using StackExchange.Redis;

namespace SaveBite.Backend.Services.Implementations;

public sealed class LocationService : ILocationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<LocationService> _logger;
    private const int GpsTtlSeconds = 300; // 5 minutes TTL for GPS metadata

    // Lua script to atomically update GEO member and GPS metadata with TTL
    private const string UpdateGpsLuaScript = @"
        redis.call('GEOADD', KEYS[1], ARGV[1], ARGV[2], ARGV[3])
        redis.call('SET', KEYS[2], ARGV[4], 'EX', ARGV[5])
        return 1
    ";

    public LocationService(IConnectionMultiplexer redis, ILogger<LocationService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<long> UpdateGpsAsync(Guid userId, double latitude, double longitude, double? accuracy, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var gpsVersion = DateTime.UtcNow.Ticks;

        var metadata = new UserGpsMetadata
        {
            Latitude = latitude,
            Longitude = longitude,
            Accuracy = accuracy,
            UpdatedAt = DateTime.UtcNow,
            GpsVersion = gpsVersion
        };

        var metadataJson = JsonSerializer.Serialize(metadata);
        var geoKey = (RedisKey)RedisKeys.UsersGpsGeo;
        var metaKey = (RedisKey)RedisKeys.UserGpsMetadata(userId);

        var keys = new[] { geoKey, metaKey };
        var args = new RedisValue[]
        {
            longitude.ToString(CultureInfo.InvariantCulture),
            latitude.ToString(CultureInfo.InvariantCulture),
            userId.ToString(),
            metadataJson,
            GpsTtlSeconds
        };

        await db.ScriptEvaluateAsync(UpdateGpsLuaScript, keys, args);
        _logger.LogDebug("User {UserId} GPS updated to {Lat},{Lon} (v{Version})", userId, latitude, longitude, gpsVersion);
        return gpsVersion;
    }

    public async Task<UserGpsMetadata?> GetGpsMetadataAsync(Guid userId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync(RedisKeys.UserGpsMetadata(userId));
        if (json.IsNullOrEmpty) return null;

        try
        {
            return JsonSerializer.Deserialize<UserGpsMetadata>(json.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize GPS metadata for user {UserId}", userId);
            return null;
        }
    }

    public async Task RemoveGpsAsync(Guid userId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await Task.WhenAll(
            db.GeoRemoveAsync(RedisKeys.UsersGpsGeo, userId.ToString()),
            db.KeyDeleteAsync(RedisKeys.UserGpsMetadata(userId))
        );
    }

    public async Task SyncDefaultAddressAsync(Guid userId, double latitude, double longitude, long addressVersion, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var member = userId.ToString();
        await Task.WhenAll(
            db.GeoAddAsync(RedisKeys.UsersDefaultGeo, longitude, latitude, member),
            db.StringSetAsync(RedisKeys.UserDefaultAddressVersion(userId), addressVersion.ToString(CultureInfo.InvariantCulture))
        );
        _logger.LogDebug("User {UserId} Default Address synced to Redis GEO (v{Version})", userId, addressVersion);
    }

    public async Task RemoveDefaultAddressAsync(Guid userId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await Task.WhenAll(
            db.GeoRemoveAsync(RedisKeys.UsersDefaultGeo, userId.ToString()),
            db.KeyDeleteAsync(RedisKeys.UserDefaultAddressVersion(userId))
        );
    }

    public async Task<long?> GetSyncedDefaultAddressVersionAsync(Guid userId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var val = await db.StringGetAsync(RedisKeys.UserDefaultAddressVersion(userId));
        if (val.IsNullOrEmpty) return null;
        return long.TryParse(val.ToString(), out var version) ? version : null;
    }
}
