using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Services.Implementations;

// Existing deals have their own visibility; hiding the catalog product does not stop an open deal.
// Customer reads always use committed PostgreSQL data. No cache lookup or cache repair.
public static class FlashDealReadQuery
{
    public static IQueryable<FlashDeal> Visible(AppDbContext db, DateTime now)
        => Visible(db.FlashDeals.AsNoTrackingWithIdentityResolution(), now);

    public static IQueryable<FlashDeal> Visible(IQueryable<FlashDeal> deals, DateTime now) => deals.Where(d => (d.Status == FlashDealStatus.OnSale || d.Status == FlashDealStatus.SoldOut)
            && d.SaleStartTime <= now && d.OrderEndTime > now && d.ShopClosingTime > now
            && d.Shop.Status == ShopStatus.Active && (d.Product.Status == ProductStatus.Active || d.Product.Status == ProductStatus.Hidden) && d.Product.DeletedAt == null);

    public static IQueryable<FlashDeal> WithDetails(IQueryable<FlashDeal> query) => query
        .Include(d => d.Shop)
        .Include(d => d.Product).ThenInclude(p => p.Images)
        .Include(d => d.Product).ThenInclude(p => p.Category)
        .Include(d => d.Product).ThenInclude(p => p.Attributes).ThenInclude(a => a.Values)
        .Include(d => d.Variants).ThenInclude(v => v.Variant).ThenInclude(v => v.VariantValues)
            .ThenInclude(v => v.AttributeValue).ThenInclude(v => v.Attribute)
        .AsSplitQuery();

    public static double Distance(double lat, double lon, double shopLat, double shopLon)
    {
        var a = Math.Pow(Math.Sin((shopLat - lat) * Math.PI / 360), 2)
            + Math.Cos(lat * Math.PI / 180) * Math.Cos(shopLat * Math.PI / 180)
            * Math.Pow(Math.Sin((shopLon - lon) * Math.PI / 360), 2);
        return 6371 * 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }
}
