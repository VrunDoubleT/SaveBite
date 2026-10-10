using Microsoft.EntityFrameworkCore;
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

    public async Task<List<Shop>> GetActiveShopsAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var query = _context.Shops
            .AsNoTracking()
            .Where(s => s.Status == ShopStatus.Active);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var cleanKeyword = keyword.Trim().ToLower();
            query = query.Where(s => s.Name.ToLower().Contains(cleanKeyword));
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
        var avgRating = totalReviews > 0 ? await reviewsQuery.AverageAsync(f => f.Rating, cancellationToken) : 5.0;

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
            .Include(f => f.Product).ThenInclude(p => p.Images)
            .Include(f => f.Reply).ThenInclude(r => r!.RepliedByUser)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }
}
