using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Caching.Interfaces;
using StackExchange.Redis;

namespace SaveBite.Backend.Caching.Implementations;

// Mirror committed PostgreSQL stock quantities into existing Redis cache entries.
public sealed class CheckoutStockCache : ICheckoutStockCache
{
    private readonly AppDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CheckoutStockCache> _logger;

    public CheckoutStockCache(AppDbContext db, IConnectionMultiplexer redis, ILogger<CheckoutStockCache> logger)
    {
        _dbContext = db;
        _redis = redis;
        _logger = logger;
    }

    // Refresh existing Redis stock hashes from authoritative PostgreSQL quantities.
    public async Task SyncAsync(Guid orderId, CancellationToken ct = default)
    {
        try
        {
            var variants = await _dbContext.Orders.Where(x => x.Id == orderId).SelectMany(x => x.Items)
                .Select(x => x.FlashDealVariant).AsNoTracking().ToListAsync(ct);
            var cache = _redis.GetDatabase();
            foreach (var group in variants.GroupBy(x => x.FlashDealId))
            {
                var key = RedisKeys.DealStock(group.Key);
                if (!await cache.KeyExistsAsync(key)) continue;
                await cache.HashSetAsync(key, group.Select(x => new HashEntry(x.VariantId.ToString(),
                    Math.Max(0L, (long)x.TotalQuantity - x.ReservedQuantity - x.SoldQuantity))).ToArray());
            }
        }
        catch (RedisException ex) { _logger.LogWarning(ex, "Could not refresh cached stock for order {OrderId}; checkout still validates PostgreSQL stock.", orderId); }
    }
}
