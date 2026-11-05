using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Synchronize derived deal status from committed PostgreSQL inventory.
public sealed class OrderInventorySynchronizer : IOrderInventorySynchronizer
{
    private readonly AppDbContext _dbContext;
    private readonly DealInventoryService _inventory;

    public OrderInventorySynchronizer(AppDbContext db, DealInventoryService inventory)
    {
        _dbContext = db;
        _inventory = inventory;
    }

    // Inventory was committed by checkout. Reconcile the display status under the deal lock.
    public async Task SyncAsync(Guid orderId, CancellationToken ct = default)
    {
        var dealIds = await _dbContext.Orders.Where(x => x.Id == orderId).SelectMany(x => x.Items)
            .Select(x => x.FlashDealVariant.FlashDealId).Distinct().OrderBy(id => id).ToListAsync(ct);
        foreach (var dealId in dealIds)
            await _inventory.ReconcileAsync(dealId, ct);
    }
}
