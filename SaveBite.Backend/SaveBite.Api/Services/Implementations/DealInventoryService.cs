using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

public sealed class DealInventoryService(AppDbContext db, IFlashDealRepository deals, IFlashDealRedisService redis, TimeProvider clock)
{
    public async Task ReconcileAsync(Guid dealId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deals WHERE id = {dealId} FOR UPDATE", ct);
        var deal = await deals.GetActiveDealByIdAsync(dealId, ct);
        if (deal == null || deal.Status is not (FlashDealStatus.OnSale or FlashDealStatus.SoldOut)) return;
        var now = clock.GetUtcNow().UtcDateTime;
        if (now < deal.SaleStartTime || now >= deal.OrderEndTime || now >= deal.ShopClosingTime) return;
        var available = deal.Variants.Any(v => v.Status == FlashDealVariantStatus.Active && v.Variant.IsActive
            && (long)v.TotalQuantity - v.ReservedQuantity - v.SoldQuantity > 0);
        var target = available && deal.Product.Status == ProductStatus.Active && deal.Shop.Status == ShopStatus.Active
            ? FlashDealStatus.OnSale : FlashDealStatus.SoldOut;
        if (deal.Status != target)
            await db.FlashDeals.Where(d => d.Id == dealId).ExecuteUpdateAsync(set => set.SetProperty(d => d.Status, target).SetProperty(d => d.UpdatedAt, now), ct);
        deal.Status = target;
        // Keep SoldOut deals in Redis; IsPurchasable will be false. Never use a stale cached response as the source.
        await redis.SaveDealAsync(FlashDealService.MapToResponse(deal), deal.Shop.Latitude, deal.Shop.Longitude);
        await db.FlashDeals.Where(d => d.Id == dealId).ExecuteUpdateAsync(set => set.SetProperty(d => d.LifecycleSyncedAt, (DateTime?)now), ct);
        await transaction.CommitAsync(ct);
    }
}
