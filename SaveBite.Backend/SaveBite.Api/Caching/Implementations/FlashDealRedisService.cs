using System.Text.Json;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Models.Enums;
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

    private IDatabase Db => _redis.GetDatabase();

    public async Task<FlashDealResponse?> GetDealAsync(Guid dealId)
    {
        var dealKey = RedisKeys.Deal(dealId);

        var entries = await Db.HashGetAllAsync(dealKey);
        if (entries.Length > 0)
        {
            var dict = entries.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

            var variantsKey = RedisKeys.DealVariants(dealId);
            var variantEntries = await Db.HashGetAllAsync(variantsKey);
            var variants = new List<FlashDealVariantResponse>();
            var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var vEntry in variantEntries)
            {
                if (!vEntry.Value.IsNullOrEmpty)
                {
                    try
                    {
                        var v = JsonSerializer.Deserialize<FlashDealVariantResponse>(vEntry.Value.ToString(), serializerOptions);
                        if (v != null) variants.Add(v);
                    }
                    catch
                    {
                    }
                }
            }

            // Synchronize real-time stock quantities from Redis key savebite:deal:{dealId}:stock.
            var stockKey = RedisKeys.DealStock(dealId);
            var stockEntries = await Db.HashGetAllAsync(stockKey);
            if (stockEntries.Length > 0)
            {
                var stockDict = stockEntries.ToDictionary(
                    x => x.Name.ToString(),
                    x => int.TryParse(x.Value.ToString(), out var qty) ? qty : 0);

                foreach (var v in variants)
                {
                    if (stockDict.TryGetValue(v.VariantId.ToString(), out var liveStock))
                    {
                        v.TotalQuantity = liveStock + v.SoldQuantity;
                    }
                }
            }

            Enum.TryParse<FlashDealStatus>(dict.GetValueOrDefault("status", "OnSale"), true, out var status);
            DateTime.TryParse(dict.GetValueOrDefault("saleStart"), null, System.Globalization.DateTimeStyles.RoundtripKind, out var saleStart);
            DateTime.TryParse(dict.GetValueOrDefault("orderEnd"), null, System.Globalization.DateTimeStyles.RoundtripKind, out var orderEnd);
            DateTime.TryParse(dict.GetValueOrDefault("shopClosingTime"), null, System.Globalization.DateTimeStyles.RoundtripKind, out var shopClosingTime);
            DateTime.TryParse(dict.GetValueOrDefault("createdAt"), null, System.Globalization.DateTimeStyles.RoundtripKind, out var createdAt);
            decimal.TryParse(dict.GetValueOrDefault("minDealPrice"), out var minDealPrice);
            decimal.TryParse(dict.GetValueOrDefault("maxOriginalPrice"), out var maxOriginalPrice);
            decimal.TryParse(dict.GetValueOrDefault("maxDiscountPercent"), out var maxDiscountPercent);
            Guid.TryParse(dict.GetValueOrDefault("shopId"), out var shopId);
            Guid.TryParse(dict.GetValueOrDefault("productId"), out var productId);

            List<string> imageUrls = new();
            if (dict.TryGetValue("productImageUrls", out var imgsJson) && !string.IsNullOrEmpty(imgsJson))
            {
                try
                {
                    imageUrls = JsonSerializer.Deserialize<List<string>>(imgsJson) ?? new();
                }
                catch { }
            }

            var attributeGroups = new List<DealAttributeGroupResponse>();
            var attrMap = new Dictionary<string, List<string>>();
            foreach (var v in variants)
            {
                if (v.Attributes != null)
                {
                    foreach (var (k, val) in v.Attributes)
                    {
                        if (!attrMap.TryGetValue(k, out var list))
                        {
                            list = new List<string>();
                            attrMap[k] = list;
                        }
                        if (!list.Contains(val))
                        {
                            list.Add(val);
                        }
                    }
                }
            }
            foreach (var (k, list) in attrMap)
            {
                attributeGroups.Add(new DealAttributeGroupResponse
                {
                    Name = k,
                    Values = list
                });
            }

            var productName = dict.GetValueOrDefault("name") ?? dict.GetValueOrDefault("productName") ?? string.Empty;

            return new FlashDealResponse
            {
                Id = dealId,
                ShopId = shopId,
                ShopName = dict.GetValueOrDefault("shopName", string.Empty),
                ShopAddress = dict.GetValueOrDefault("shopAddress", string.Empty),
                ShopLogoUrl = dict.GetValueOrDefault("shopLogoUrl"),
                ProductId = productId,
                ProductName = productName,
                ProductImageUrl = dict.GetValueOrDefault("productImageUrl"),
                ProductImageUrls = imageUrls,
                CategoryName = dict.GetValueOrDefault("categoryName"),
                Description = dict.GetValueOrDefault("description"),
                SaleStartTime = saleStart,
                OrderEndTime = orderEnd,
                ShopClosingTime = shopClosingTime,
                CreatedAt = createdAt != default ? createdAt : saleStart,
                Status = status,
                MinDealPrice = minDealPrice,
                MaxOriginalPrice = maxOriginalPrice,
                MaxDiscountPercent = maxDiscountPercent,
                AttributeGroups = attributeGroups,
                Variants = variants
            };
        }

        var json = await Db.StringGetAsync(dealKey);
        if (!json.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<FlashDealResponse>(json.ToString());
        }

        return null;
    }

    public async Task<List<FlashDealResponse>> GetDealsByShopAsync(Guid shopId)
    {
        var shopDealsKey = RedisKeys.ShopDeals(shopId);

        var nowScore = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var dealIds = await Db.SortedSetRangeByScoreAsync(
            shopDealsKey,
            start: nowScore,
            stop: double.PositiveInfinity);

        if (dealIds.Length == 0)
        {
            dealIds = await Db.SortedSetRangeByRankAsync(shopDealsKey);
        }

        if (dealIds.Length == 0)
        {
            dealIds = await Db.SetMembersAsync(shopDealsKey);
        }

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
            RedisKeys.GeoActiveShops,
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

        var dealKey = RedisKeys.Deal(deal.Id);
        var dealEntries = new HashEntry[]
        {
            new("id", deal.Id.ToString()),
            new("shopId", deal.ShopId.ToString()),
            new("name", deal.ProductName),
            new("productName", deal.ProductName),
            new("shopName", deal.ShopName),
            new("shopAddress", deal.ShopAddress),
            new("shopLogoUrl", deal.ShopLogoUrl ?? string.Empty),
            new("productId", deal.ProductId.ToString()),
            new("productImageUrl", deal.ProductImageUrl ?? string.Empty),
            new("productImageUrls", JsonSerializer.Serialize(deal.ProductImageUrls)),
            new("categoryName", deal.CategoryName ?? string.Empty),
            new("description", deal.Description ?? string.Empty),
            new("status", deal.Status.ToString()),
            new("saleStart", deal.SaleStartTime.ToString("o")),
            new("orderEnd", deal.OrderEndTime.ToString("o")),
            new("shopClosingTime", deal.ShopClosingTime.ToString("o")),
            new("createdAt", deal.CreatedAt.ToString("o")),
            new("minDealPrice", deal.MinDealPrice.ToString()),
            new("maxOriginalPrice", deal.MaxOriginalPrice.ToString()),
            new("maxDiscountPercent", deal.MaxDiscountPercent.ToString())
        };
        await Db.HashSetAsync(dealKey, dealEntries);
        await Db.KeyExpireAsync(dealKey, expiryTime);

        var variantsKey = RedisKeys.DealVariants(deal.Id);
        if (deal.Variants != null && deal.Variants.Count > 0)
        {
            var variantEntries = deal.Variants.Select(v => new HashEntry(
                v.VariantId.ToString(),
                JsonSerializer.Serialize(v)
            )).ToArray();
            await Db.HashSetAsync(variantsKey, variantEntries);
            await Db.KeyExpireAsync(variantsKey, expiryTime);
        }

        var stockKey = RedisKeys.DealStock(deal.Id);
        if (deal.Variants != null && deal.Variants.Count > 0)
        {
            var stockEntries = deal.Variants.Select(v => new HashEntry(
                v.VariantId.ToString(),
                v.AvailableQuantity
            )).ToArray();
            await Db.HashSetAsync(stockKey, stockEntries);
            await Db.KeyExpireAsync(stockKey, expiryTime);
        }

        var shopDealsKey = RedisKeys.ShopDeals(deal.ShopId);
        var utcOrderEnd = deal.OrderEndTime.Kind == DateTimeKind.Utc
            ? deal.OrderEndTime
            : DateTime.SpecifyKind(deal.OrderEndTime, DateTimeKind.Utc);
        var orderEndScore = new DateTimeOffset(utcOrderEnd).ToUnixTimeSeconds();
        await Db.SortedSetAddAsync(shopDealsKey, deal.Id.ToString(), orderEndScore);
        await Db.KeyExpireAsync(shopDealsKey, expiryTime);

        await Db.GeoAddAsync(RedisKeys.GeoActiveShops, shopLon, shopLat, deal.ShopId.ToString());
    }

    public async Task<bool> DealExistsAsync(Guid dealId)
    {
        var dealKey = RedisKeys.Deal(dealId);
        return await Db.KeyExistsAsync(dealKey);
    }

    public async Task DeleteDealAsync(Guid dealId, Guid shopId)
    {
        var dealKey = RedisKeys.Deal(dealId);
        var variantsKey = RedisKeys.DealVariants(dealId);
        var stockKey = RedisKeys.DealStock(dealId);
        var shopDealsKey = RedisKeys.ShopDeals(shopId);

        await Db.KeyDeleteAsync(dealKey);
        await Db.KeyDeleteAsync(variantsKey);
        await Db.KeyDeleteAsync(stockKey);
        await Db.SortedSetRemoveAsync(shopDealsKey, dealId.ToString());

        var remainingDeals = await Db.SortedSetLengthAsync(shopDealsKey);
        if (remainingDeals == 0)
        {
            await Db.GeoRemoveAsync(RedisKeys.GeoActiveShops, shopId.ToString());
        }
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
