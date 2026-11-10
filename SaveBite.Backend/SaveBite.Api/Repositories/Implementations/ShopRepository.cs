using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class ShopRepository : IShopRepository
{
    private readonly AppDbContext _context;

    public ShopRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Shop?> GetShopByOwnerUserIdAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(shop => shop.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task<List<Shop>> GetActiveShopsAsync(string? keyword, string? category = null, string? shopName = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Shops
            .AsNoTracking()
            .Include(s => s.FlashDeals)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.Category)
            .Where(s => s.Status == ShopStatus.Active);

        if (!string.IsNullOrWhiteSpace(shopName))
        {
            var rawShop = SaveBite.Backend.Helpers.VietnameseTextHelper.NormalizeNfc(shopName).ToLowerInvariant();
            var cleanShop = SaveBite.Backend.Helpers.VietnameseTextHelper.RemoveDiacritics(shopName);
            query = query.Where(s => s.Name.ToLower() == rawShop || EF.Functions.Unaccent(s.Name).ToLower() == cleanShop);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var rawKw = SaveBite.Backend.Helpers.VietnameseTextHelper.NormalizeNfc(keyword).ToLowerInvariant();
            var cleanKw = SaveBite.Backend.Helpers.VietnameseTextHelper.RemoveDiacritics(keyword);
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, $"%{rawKw}%") ||
                EF.Functions.ILike(EF.Functions.Unaccent(s.Name), $"%{cleanKw}%") ||
                EF.Functions.ILike(s.AddressLine, $"%{rawKw}%") ||
                EF.Functions.ILike(EF.Functions.Unaccent(s.AddressLine), $"%{cleanKw}%") ||
                s.FlashDeals.Any(d => d.Status == FlashDealStatus.OnSale &&
                    (EF.Functions.ILike(d.Product.Name, $"%{rawKw}%") ||
                     EF.Functions.ILike(EF.Functions.Unaccent(d.Product.Name), $"%{cleanKw}%") ||
                     EF.Functions.ILike(d.Product.Category.Name, $"%{rawKw}%") ||
                     EF.Functions.ILike(EF.Functions.Unaccent(d.Product.Category.Name), $"%{cleanKw}%"))));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var rawCategories = category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                        .Select(c => SaveBite.Backend.Helpers.VietnameseTextHelper.NormalizeNfc(c).ToLowerInvariant())
                                        .ToList();
            var unaccentCategories = category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                             .Select(c => SaveBite.Backend.Helpers.VietnameseTextHelper.RemoveDiacritics(c))
                                             .ToList();
            if (rawCategories.Count > 0)
            {
                query = query.Where(s => s.FlashDeals.Any(d => d.Status == FlashDealStatus.OnSale &&
                    (rawCategories.Contains(d.Product.Category.Name.ToLower()) ||
                     unaccentCategories.Contains(EF.Functions.Unaccent(d.Product.Category.Name).ToLower()))));
            }
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Shop?> GetActiveShopByIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shopId && s.Status == ShopStatus.Active, cancellationToken);
    }

    public async Task<bool> ShopExistsAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .AsNoTracking()
            .AnyAsync(s => s.Id == shopId, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> GetActiveDealCountsForShopsAsync(
        List<Guid> shopIds,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<FlashDeal>()
            .AsNoTracking()
            .Where(d => shopIds.Contains(d.ShopId)
                && (d.Status == FlashDealStatus.OnSale || d.Status == FlashDealStatus.SoldOut)
                && d.OrderEndTime >= now)
            .GroupBy(d => d.ShopId)
            .Select(g => new
            {
                ShopId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.ShopId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<Guid, (double AvgRating, int TotalCount)>> GetReviewsStatsForShopsAsync(
        List<Guid> shopIds, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductFeedback>()
            .AsNoTracking()
            .Where(f => shopIds.Contains(f.Product.ShopId) && f.Status == FeedbackStatus.Visible)
            .GroupBy(f => f.Product.ShopId)
            .Select(g => new
            {
                ShopId = g.Key,
                AvgRating = g.Average(x => x.Rating),
                Total = g.Count()
            })
            .ToDictionaryAsync(
                x => x.ShopId, 
                x => (x.AvgRating, x.Total), 
                cancellationToken);
    }

    public async Task<(double AvgRating, int TotalReviews, Dictionary<int, int> RatingBreakdown)> GetShopReviewStatsAsync(
        Guid shopId, 
        CancellationToken cancellationToken = default)
    {
        var reviewsQuery = _context.Set<ProductFeedback>()
            .AsNoTracking()
            .Where(f => f.Product.ShopId == shopId && f.Status == FeedbackStatus.Visible);

        var totalReviews = await reviewsQuery.CountAsync(cancellationToken);
        var avgRating = totalReviews > 0 ? await reviewsQuery.AverageAsync(f => f.Rating, cancellationToken) : 0.0;

        var breakdownGroups = await reviewsQuery
            .GroupBy(f => f.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Rating, x => x.Count, cancellationToken);

        var breakdown = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++)
        {
            breakdown[i] = breakdownGroups.GetValueOrDefault(i, 0);
        }

        return (avgRating, totalReviews, breakdown);
    }

    public async Task<(List<ProductFeedback> Items, int TotalItems)> GetPagedStoreReviewsAsync(
        Guid shopId,
        int? rating,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ProductFeedback>()
            .AsNoTracking()
            .Where(f => f.Product.ShopId == shopId && f.Status == FeedbackStatus.Visible);

        if (rating is >= 1 and <= 5)
        {
            query = query.Where(f => f.Rating == rating.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(f => f.User)
            .Include(f => f.Images)
            .Include(f => f.Product).ThenInclude(p => p.Images)
            .Include(f => f.Reply).ThenInclude(r => r!.RepliedByUser)
            .AsSplitQuery()
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }
    public async Task<ShopPaymentConfig?> GetPaymentConfigByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ShopPaymentConfig>()
            .FirstOrDefaultAsync(config => config.Shop.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Shop?> GetShopProfileByOwnerUserIdAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Shops
            .AsNoTracking()
            .Include(shop => shop.Application)
            .ThenInclude(application => application.Documents.Where(document => document.IsCurrent))
            .FirstOrDefaultAsync(shop => shop.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task UpdateShopAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        _context.Shops.Update(shop);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
    // Manage Shops.
    public async Task<(IReadOnlyList<Shop> Items, int Total)> GetShopListAsync(string? keyword, int page, int pageSize, CancellationToken ct)
    {
        var query = _context.Shops.AsNoTracking()
            .Include(s => s.Owner).AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(shop => EF.Functions.ILike(shop.Name, $"%{keyword.Trim()}%"));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
    
    // Manage Shops.
    public async Task<Shop?> GetShopContentAsync(Guid shopId, CancellationToken ct)
    {
        return await _context.Shops.AsNoTracking()
            .Include(s => s.Owner)
            .Include(s => s.Application)
            .ThenInclude(a => a.Documents.Where(d => d.IsCurrent))
            .AsSplitQuery()
            .SingleOrDefaultAsync(s => s.Id == shopId, ct);
    }
    
    // Manage Shops.
    public async Task<IReadOnlyList<ShopStatusHistory>> GetShopStatusHistoryAsync(Guid shopId, CancellationToken ct)
    {
        return await _context.ShopStatusHistories.AsNoTracking()
            .Where(log => log.ShopId == shopId)
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .ToListAsync(ct);
    }
    
    // Manage Shops.
    public async Task<Shop?> GetShopForStatusUpdateAsync(Guid shopId, CancellationToken ct)
    {
        return await _context.Shops.FirstOrDefaultAsync(x => x.Id == shopId, ct);
    }
}
