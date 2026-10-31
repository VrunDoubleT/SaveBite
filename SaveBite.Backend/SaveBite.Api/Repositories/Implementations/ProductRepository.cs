using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public async Task<ProductOptionsResponse> GetOptionsAsync(Guid userId, CancellationToken ct)
    {
        var shop = await GetAvailableShopAsync(userId, ct);
        var categories = await _db.Categories.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new ProductCategoryOption(x.Id, x.Name)).ToArrayAsync(ct);
        return new ProductOptionsResponse(shop, categories);
    }

    public async Task<ProductShopOption?> GetAvailableShopAsync(Guid userId, CancellationToken ct)
    {
        var shops = await _db.Shops.AsNoTracking()
            .Where(x => x.OwnerUserId == userId)
            .Select(x => new { x.Id, x.Name, x.Status }).Take(2).ToArrayAsync(ct);
        if (shops.Length > 1)
            throw AppException.Conflict("This owner has multiple shops. Each owner must have exactly one shop.");
        var shop = shops.SingleOrDefault();
        return shop is null ? null : new ProductShopOption(shop.Id, shop.Name);
    }

    public async Task<ProductSummaryResponse> GetSummaryAsync(Guid shopId, CancellationToken ct)
        => await _db.Products.AsNoTracking()
            .Where(x => x.ShopId == shopId && x.DeletedAt == null)
            .GroupBy(x => x.ShopId)
            .Select(g => new ProductSummaryResponse(g.Count(),
                g.Count(x => x.Status == ProductStatus.Active),
                g.Count(x => x.Status == ProductStatus.Hidden),
                g.Sum(x => x.Variants.Count)))
            .SingleOrDefaultAsync(ct) ?? new ProductSummaryResponse(0, 0, 0, 0);

    public Task<bool> ActiveCategoryExistsAsync(Guid categoryId, CancellationToken ct)
        => _db.Categories.AnyAsync(x => x.Id == categoryId && x.IsActive, ct);

    public Task<bool> AnyValueIdsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
        => _db.ProductAttributeValues.AnyAsync(x => ids.Contains(x.Id), ct);

    public Task<Product?> GetOwnedAsync(Guid userId, Guid shopId, Guid productId,
        bool includeDeleted, CancellationToken ct)
        => Details(_db.Products).SingleOrDefaultAsync(x => x.Id == productId &&
            x.ShopId == shopId && x.Shop.OwnerUserId == userId &&
            (includeDeleted || x.DeletedAt == null), ct);


    private static IQueryable<Product> Details(IQueryable<Product> query)
        => query.Include(x => x.Category).Include(x => x.Images)
            .Include(x => x.Attributes).ThenInclude(x => x.Values)
            .Include(x => x.Variants).ThenInclude(x => x.VariantValues)
            .Include(x => x.Warnings.Where(w => w.ResolvedAt == null))
            .Include(x => x.ContentRevisions.OrderByDescending(r => r.RevisionNumber).Take(1))
            .AsSplitQuery();

    public async Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(
        Guid shopId, ProductListRequest request, CancellationToken ct)
    {
        var query = _db.Products.AsNoTracking().Where(x => x.ShopId == shopId && x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(search));
        }
        if (request.CategoryId.HasValue) query = query.Where(x => x.CategoryId == request.CategoryId);
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        var total = await query.CountAsync(ct);
        var offset = ((long)request.Page - 1) * request.PageSize;
        if (offset >= total) return (Array.Empty<Product>(), total);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((int)offset).Take(request.PageSize)
            .Include(x => x.Category).Include(x => x.Images)
            .Include(x => x.Attributes).ThenInclude(x => x.Values)
            .Include(x => x.Variants).AsSplitQuery().ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<FlashDeal> Items, int Total)> ListFlashDealsAsync(
        Guid shopId, Guid productId, ProductFlashDealListRequest request, CancellationToken ct)
    {
        var query = _db.FlashDeals.AsNoTracking().Where(x => x.ShopId == shopId && x.ProductId == productId);
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        var total = await query.CountAsync(ct);
        var offset = ((long)request.Page - 1) * request.PageSize;
        if (offset >= total) return (Array.Empty<FlashDeal>(), total);
        var items = await query.OrderByDescending(x => x.SaleStartTime).ThenBy(x => x.Id)
            .Skip((int)offset).Take(request.PageSize)
            .Include(x => x.Variants).ThenInclude(x => x.Variant).ThenInclude(x => x.VariantValues)
            .ThenInclude(x => x.AttributeValue).ThenInclude(x => x.Attribute)
            .AsSplitQuery().ToListAsync(ct);
        return (items, total);
    }

    public void Add(Product product) => _db.Products.Add(product);
    public void AddValues(IEnumerable<ProductAttributeValue> values) => _db.ProductAttributeValues.AddRange(values);
    public void AddVariants(IEnumerable<ProductVariant> variants) => _db.ProductVariants.AddRange(variants);
    public void AddImages(IEnumerable<ProductImage> images) => _db.ProductImages.AddRange(images);
    public void RemoveImages(IEnumerable<ProductImage> images) => _db.ProductImages.RemoveRange(images);
    public async Task<decimal?> GetMaxActiveFlashDealPriceForVariantAsync(Guid variantId, CancellationToken ct)
        => await _db.FlashDealVariants.AsNoTracking()
            .Where(fdv => fdv.VariantId == variantId &&
                          (fdv.FlashDeal.Status == FlashDealStatus.Pending || fdv.FlashDeal.Status == FlashDealStatus.OnSale))
            .Select(fdv => (decimal?)fdv.DealPrice)
            .MaxAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw AppException.Conflict("The product changed during this request. Reload it and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException
        { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            throw AppException.Conflict("One of the supplied identifiers already exists. Reload and try again.");
        }
    }

    // Manage Products.
    public async Task<(IReadOnlyList<Product> Items, int Total)> GetProductListAsync(string? keyword, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Products.AsNoTracking().Where(p => p.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var search = $"%{keyword.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, search) ||
                EF.Functions.ILike(p.Shop.Name, search));
        }

        var total = await query.CountAsync(ct);
        var offset = ((long)page - 1) * pageSize;
        if (offset >= total) return (Array.Empty<Product>(), total);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .Include(p => p.Shop)
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.Warnings.Where(w => w.ResolvedAt == null))
            .Include(p => p.ContentRevisions
                .OrderByDescending(revision => revision.RevisionNumber)
                .Take(1))
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, total);
    }

    // Manage Products.
    public async Task<Product?> GetProductContentAsync(Guid productId, CancellationToken ct)
    {
        return await _db.Products.AsNoTrackingWithIdentityResolution()
            .Include(p => p.Shop).Include(p => p.Category).Include(p => p.Images)
            .Include(p => p.Attributes).ThenInclude(a => a.Values)
            .Include(p => p.Variants).ThenInclude(v => v.VariantValues)
            .Include(p => p.Warnings)
            .Include(p => p.ContentRevisions).ThenInclude(r => r.Category)
            .Include(p => p.ContentRevisions).ThenInclude(r => r.SubmittedByUser)
            .Include(p => p.ContentRevisions).ThenInclude(r => r.ReviewedByUser)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == productId && p.DeletedAt == null, ct);
    }

    // Manage Products.
    public async Task<Product?> GetProductForViolationRemovalAsync(Guid productId, CancellationToken ct)
    {
        return await _db.Products.FirstOrDefaultAsync(x => x.Id == productId, ct);
    }

    // Manage Products.
    public async Task RemoveProductForViolationAndCancelDealsAsync(Product product, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        await _db.FlashDeals
            .Where(x =>
                x.ProductId == product.Id &&
                x.Status != FlashDealStatus.Cancelled &&
                x.Status != FlashDealStatus.Closed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        FlashDealStatus.Cancelled)
                    .SetProperty(
                        x => x.UpdatedAt,
                        DateTime.UtcNow),
                ct);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    // Manage Products.
    public async Task<ProductContentRevision?> GetLatestRevisionForReviewAsync(Guid productId, CancellationToken ct)
    {
        return await _db.ProductContentRevisions.OrderByDescending(x => x.RevisionNumber)
            .FirstOrDefaultAsync(x => x.ProductId == productId, ct);
    }

    // Manage Products.
    public void AddContentRevision(ProductContentRevision revision) => _db.ProductContentRevisions.Add(revision);
}
