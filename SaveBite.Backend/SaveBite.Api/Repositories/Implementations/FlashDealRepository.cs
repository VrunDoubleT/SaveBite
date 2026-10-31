using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;

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
            .AsNoTrackingWithIdentityResolution()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Product).ThenInclude(p => p.Category)
            .Include(d => d.Product).ThenInclude(p => p.Attributes).ThenInclude(a => a.Values)
            .Include(d => d.Variants).ThenInclude(v => v.Variant).ThenInclude(pv => pv.VariantValues).ThenInclude(vv => vv.AttributeValue).ThenInclude(av => av.Attribute)
            .FirstOrDefaultAsync(d => d.Id == dealId, cancellationToken);
    }

    public async Task<List<FlashDeal>> GetActiveDealsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<FlashDeal>()
            .AsNoTrackingWithIdentityResolution()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Product).ThenInclude(p => p.Category)
            .Include(d => d.Variants).ThenInclude(v => v.Variant)
            .Where(d => d.ShopId == shopId
                && (d.Status == FlashDealStatus.OnSale || d.Status == FlashDealStatus.SoldOut)
                && d.OrderEndTime >= now)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FlashDeal>> GetAllActiveDealsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<FlashDeal>()
            .AsNoTrackingWithIdentityResolution()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Product).ThenInclude(p => p.Category)
            .Include(d => d.Variants).ThenInclude(v => v.Variant)
            .Where(d => (d.Status == FlashDealStatus.OnSale || d.Status == FlashDealStatus.SoldOut)
                && d.Shop != null && d.Shop.Status == ShopStatus.Active
                && d.OrderEndTime >= now)
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
            query = query.Where(d => d.CreatedAt < cursor1.Value);
        }
        else if (mode == CursorMode.Newer && cursor2.HasValue)
        {
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

    // Retrieve paginated flash deals of a specific shop with optional filters.
    public async Task<PagedResult<FlashDeal>> GetShopDealsAsync(
        Guid shopId,
        ShopFlashDealQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<FlashDeal>()
            .AsNoTracking()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Product).ThenInclude(p => p.Category)
            .Include(d => d.Variants).ThenInclude(v => v.Variant)
            .Where(d => d.ShopId == shopId);

        if (request.Status.HasValue)
        {
            query = query.Where(d => d.Status == request.Status.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(d => d.SaleStartTime >= request.FromDate.Value);
        }
        if (request.ToDate.HasValue)
        {
            query = query.Where(d => d.OrderEndTime <= request.ToDate.Value);
        }
        var totalItems = await query.CountAsync(cancellationToken);
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<FlashDeal>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }
    // Retrieve full detail of a specific flash deal belonging to a shop (including attributes and variant values).
    public async Task<FlashDeal?> GetShopDealByIdAsync(
        Guid shopId,
        Guid dealId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<FlashDeal>()
            .Include(d => d.Shop)
            .Include(d => d.Product).ThenInclude(p => p.Images)
            .Include(d => d.Product).ThenInclude(p => p.Category)
            .Include(d => d.Product).ThenInclude(p => p.Attributes).ThenInclude(a => a.Values)
            .Include(d => d.Variants).ThenInclude(v => v.Variant).ThenInclude(pv => pv.VariantValues).ThenInclude(vv => vv.AttributeValue).ThenInclude(av => av.Attribute)
            .FirstOrDefaultAsync(d => d.Id == dealId && d.ShopId == shopId, cancellationToken);
    }
    // Check if there is an existing overlapping deal for the same product in this shop.
    public async Task<bool> HasOverlappingDealAsync(
        Guid shopId,
        Guid productId,
        DateTime startTime,
        DateTime endTime,
        Guid? excludeDealId = null,
        CancellationToken cancellationToken = default)
    {
        var activeStatuses = new[] { FlashDealStatus.Pending, FlashDealStatus.OnSale, FlashDealStatus.Hidden };
        var query = _context.Set<FlashDeal>()
            .AsNoTracking()
            .Where(d => d.ShopId == shopId
                && d.ProductId == productId
                && activeStatuses.Contains(d.Status)
                && d.SaleStartTime < endTime
                && d.OrderEndTime > startTime);
        if (excludeDealId.HasValue)
        {
            query = query.Where(d => d.Id != excludeDealId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }
    // Retrieve active product and its variants to validate deal creation.
    public async Task<Product?> GetProductForDealCreationAsync(
        Guid shopId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Product>()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId && p.ShopId == shopId, cancellationToken);
    }
    // Retrieve active products with variants and categories for flash deal creation dropdown.
    public async Task<List<Product>> GetShopEligibleProductsAsync(
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<Product>()
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants).ThenInclude(v => v.VariantValues).ThenInclude(vv => vv.AttributeValue)
            .Where(p => p.ShopId == shopId && p.Status == ProductStatus.Active && p.Variants.Any(v => v.IsActive))
            .Where(p => !_context.ProductWarnings.Where(WarningDealPolicy.BlockedAt(DateTime.UtcNow)).Any(w => w.ProductId == p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    // Insert a new flash deal into the database.
    public async Task AddAsync(FlashDeal deal, CancellationToken cancellationToken = default)
    {
        await _context.Set<FlashDeal>().AddAsync(deal, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
    // Update an existing flash deal in the database.
    public async Task UpdateAsync(FlashDeal deal, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(deal);
        if (entry.State == EntityState.Detached)
        {
            _context.Set<FlashDeal>().Attach(deal);
            entry.State = EntityState.Modified;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Direct atomic status update avoiding change tracking graph conflicts
    public async Task UpdateStatusAsync(Guid dealId, FlashDealStatus status, CancellationToken cancellationToken = default)
    {
        await _context.Set<FlashDeal>()
            .Where(d => d.Id == dealId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, status)
                .SetProperty(d => d.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }
}
