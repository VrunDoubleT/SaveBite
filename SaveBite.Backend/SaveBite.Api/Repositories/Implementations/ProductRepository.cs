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
        return new ProductOptionsResponse(shop, Array.Empty<ProductCategoryOption>());
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
}
