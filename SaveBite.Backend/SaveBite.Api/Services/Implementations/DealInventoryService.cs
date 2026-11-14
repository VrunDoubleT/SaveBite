using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

public sealed class DealInventoryService(AppDbContext db, IFlashDealRepository deals, TimeProvider clock)
{
    public async Task ReconcileAsync(Guid dealId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deals WHERE id = {dealId} FOR UPDATE", ct);
        var deal = await deals.GetActiveDealByIdAsync(dealId, ct);
        if (deal == null) return;
        var now = clock.GetUtcNow().UtcDateTime;
        if (deal.Status is not (FlashDealStatus.OnSale or FlashDealStatus.SoldOut)
            || now < deal.SaleStartTime || now >= deal.OrderEndTime || now >= deal.ShopClosingTime)
        {
            // Stock decisions still update shop management after a deal ends or is hidden.
            FlashDealRealtimeOutbox.Stage(db, deal.Id, deal.ShopId);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return;
        }
        var available = deal.Variants.Any(v => v.Status == FlashDealVariantStatus.Active && v.Variant.IsActive
            && (long)v.TotalQuantity - v.ReservedQuantity - v.SoldQuantity > 0);
        var target = available && deal.Product.DeletedAt == null && deal.Shop.Status == ShopStatus.Active
            ? FlashDealStatus.OnSale : FlashDealStatus.SoldOut;
        if (deal.Status != target)
        {
            db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
            {
                FlashDealId = deal.Id,
                FromStatus = deal.Status,
                ToStatus = target,
                ChangedByType = "System",
                Note = target == FlashDealStatus.SoldOut
                    ? "Deal marked sold out because no sellable inventory is available."
                    : "Deal returned to sale after sellable inventory became available.",
                CreatedAt = now
            });
            await db.FlashDeals.Where(d => d.Id == dealId).ExecuteUpdateAsync(set => set.SetProperty(d => d.Status, target).SetProperty(d => d.UpdatedAt, now), ct);
        }
        deal.Status = target;
        // Both status and inventory are read directly from PostgreSQL by customers.
        await db.FlashDeals.Where(d => d.Id == dealId).ExecuteUpdateAsync(set => set.SetProperty(d => d.LifecycleSyncedAt, (DateTime?)now), ct);
        FlashDealRealtimeOutbox.Stage(db, deal.Id, deal.ShopId);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
