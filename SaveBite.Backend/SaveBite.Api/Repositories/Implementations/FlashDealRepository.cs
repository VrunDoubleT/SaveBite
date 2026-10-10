using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class FlashDealRepository : IFlashDealRepository
{
    private readonly AppDbContext _context;

    public FlashDealRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FlashDeal?> GetActiveDealByIdAsync(Guid dealId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FlashDeal>()
            .AsNoTracking()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Product).ThenInclude(p => p.Category)
            .Include(d => d.Product).ThenInclude(p => p.Attributes).ThenInclude(a => a.Values)
            .Include(d => d.Product).ThenInclude(p => p.Variants).ThenInclude(pv => pv.VariantValues).ThenInclude(vv => vv.AttributeValue).ThenInclude(av => av.Attribute)
            .Include(d => d.Variants).ThenInclude(v => v.Variant).ThenInclude(pv => pv.VariantValues).ThenInclude(vv => vv.AttributeValue).ThenInclude(av => av.Attribute)
            .FirstOrDefaultAsync(d => d.Id == dealId, cancellationToken);
    }

    public async Task<List<FlashDeal>> GetActiveDealsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<FlashDeal>()
            .AsNoTracking()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Product).ThenInclude(p => p.Category)
            .Include(d => d.Product).ThenInclude(p => p.Variants)
            .Include(d => d.Variants).ThenInclude(v => v.Variant)
            .Where(d => d.ShopId == shopId && d.Status == FlashDealStatus.OnSale)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FlashDeal>> GetAllActiveDealsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<FlashDeal>()
            .AsNoTracking()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Product).ThenInclude(p => p.Category)
            .Include(d => d.Product).ThenInclude(p => p.Variants)
            .Include(d => d.Variants).ThenInclude(v => v.Variant)
            .Where(d => d.Status == FlashDealStatus.OnSale)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<FlashDeal> Deals, bool HasOlder)> GetDealsByCursorAsync(
        DateTime? cursor1,
        DateTime? cursor2,
        CursorMode mode,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FlashDeal>()
            .AsNoTracking()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Product).ThenInclude(p => p.Category)
            .Include(d => d.Variants).ThenInclude(v => v.Variant)
            .Where(d => d.Status == FlashDealStatus.OnSale);

        if (mode == CursorMode.Older && cursor1.HasValue)
        {
            // Con trỏ 1: Lấy các deal cũ hơn mốc Cursor1
            query = query.Where(d => d.CreatedAt < cursor1.Value);
        }
        else if (mode == CursorMode.Newer && cursor2.HasValue)
        {
            // Con trỏ 2: So sánh mốc mới nhất xem có deal nào mới xuất hiện không
            query = query.Where(d => d.CreatedAt > cursor2.Value);
        }

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var minCreatedAt = items.Count > 0 ? items.Min(d => d.CreatedAt) : cursor1;

        var hasOlder = minCreatedAt.HasValue && await _context.Set<FlashDeal>()
            .AnyAsync(d => d.Status == FlashDealStatus.OnSale && d.CreatedAt < minCreatedAt.Value, cancellationToken);

        return (items, hasOlder);
    }
}
