using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Caching.Interfaces;
using StackExchange.Redis;

namespace SaveBite.Backend.Caching.Implementations;

// Synchronize committed stock through the same locked writer as RabbitMQ consumers.
public sealed class CheckoutStockCache : ICheckoutStockCache
{
    private readonly AppDbContext _dbContext;
    private readonly SaveBite.Backend.Services.Implementations.DealInventoryService _inventory;
    private readonly ILogger<CheckoutStockCache> _logger;

    public CheckoutStockCache(AppDbContext db, SaveBite.Backend.Services.Implementations.DealInventoryService inventory, ILogger<CheckoutStockCache> logger)
    {
        _dbContext = db;
        _inventory = inventory;
        _logger = logger;
    }

    // Re-read authoritative PostgreSQL quantities after acquiring each deal lock.
    public async Task SyncAsync(Guid orderId, CancellationToken ct = default)
    {
        try
        {
            var dealIds = await _dbContext.Orders.Where(x => x.Id == orderId).SelectMany(x => x.Items)
                .Select(x => x.FlashDealVariant.FlashDealId).Distinct().OrderBy(id => id).ToListAsync(ct);
            // The same parent lock used by lifecycle consumers serializes every stock cache writer.
            // Read stock again after acquiring the lock; never write this earlier snapshot.
            foreach (var dealId in dealIds)
                await _inventory.ReconcileAsync(dealId, ct);
        }
        catch (RedisException ex) { _logger.LogWarning(ex, "Could not refresh cached stock for order {OrderId}; checkout still validates PostgreSQL stock.", orderId); }
    }
}
