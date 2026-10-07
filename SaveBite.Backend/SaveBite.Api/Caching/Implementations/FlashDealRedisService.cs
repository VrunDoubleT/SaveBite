using System.Text.Json;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Models.Responses;
using StackExchange.Redis;

namespace SaveBite.Backend.Caching.Implementations;

public sealed class FlashDealRedisService : IFlashDealRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<FlashDealRedisService> _logger;

    public FlashDealRedisService(IConnectionMultiplexer redis, ILogger<FlashDealRedisService> logger)
    {
        _redis = redis;
        _logger = logger;
    }
    private IDatabase Db  => _redis.GetDatabase();    
    
    public async Task<FlashDealResponse?> GetDealAsync(Guid dealId)
    {
        var json = await Db.StringGetAsync(RedisKeys.FlashDealDetail(dealId));
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<FlashDealResponse>(json.ToString());
    }

    public async Task<List<FlashDealResponse>> GetDealsByShopAsync(Guid shopId)
    {
        var dealIds = await Db.SetMembersAsync(RedisKeys.FlashDealShopDeals(shopId));
        if (dealIds.Length == 0) return new List<FlashDealResponse>();
        var results = new List<FlashDealResponse>();
        foreach (var id in dealIds)
        {
            if (Guid.TryParse(id.ToString(), out var dealId))
            {
                var deal = await GetDealAsync(dealId);
                if (deal != null) results.Add(deal);
            }
        }
        return results;
    }
  public async Task<List<FlashDealResponse>> GetNearbyDealsAsync(double latitude, double longitude, double radiusInKm)
    {

        var geoResults = await Db.GeoRadiusAsync(
            RedisKeys.FlashDealGeoShops,
            longitude,
            latitude,
            radiusInKm,
            GeoUnit.Kilometers,
            order: Order.Ascending,
            options: GeoRadiusOptions.WithDistance);
        var nearbyDeals = new List<FlashDealResponse>();

        foreach (var result in geoResults)
        {
            if (!Guid.TryParse(result.Member.ToString(), out var shopId)) continue;
            var deals = await GetDealsByShopAsync(shopId);
            foreach (var deal in deals)
            {
                deal.DistanceInKm = Math.Round(result.Distance ?? 0, 2);
                nearbyDeals.Add(deal);
            }
        }
        return nearbyDeals;
    }
    public async Task SaveDealAsync(FlashDealResponse deal, double shopLat, double shopLon, TimeSpan? expiry = null)
    {
        var expiryTime = expiry ?? TimeSpan.FromHours(12);
        await Db.StringSetAsync(RedisKeys.FlashDealDetail(deal.Id), JsonSerializer.Serialize(deal), expiryTime);

        await Db.SetAddAsync(RedisKeys.FlashDealShopDeals(deal.ShopId), deal.Id.ToString());

        await Db.SetAddAsync(RedisKeys.FlashDealActiveDeals, deal.Id.ToString());
 
        await Db.GeoAddAsync(RedisKeys.FlashDealGeoShops, shopLon, shopLat, deal.ShopId.ToString());
    }
    public async Task SeedFakeDealsAsync(List<FlashDealResponse> deals, List<(Guid ShopId, double Lat, double Lon)> shopLocations)
    {
        var locationMap = shopLocations.ToDictionary(x => x.ShopId, x => (x.Lat, x.Lon));
        foreach (var deal in deals)
        {
            if (locationMap.TryGetValue(deal.ShopId, out var loc))
            {
                await SaveDealAsync(deal, loc.Lat, loc.Lon);
            }
        }
    }
}