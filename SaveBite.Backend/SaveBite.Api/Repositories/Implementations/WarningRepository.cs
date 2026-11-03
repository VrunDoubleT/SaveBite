using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class WarningRepository : IWarningRepository
{
    private readonly AppDbContext _db;

    public WarningRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<ProductContentRevision?> GetLatestRevisionForWarningAsync(Guid productId, CancellationToken ct)
        => _db.ProductContentRevisions.OrderByDescending(x => x.RevisionNumber)
            .FirstOrDefaultAsync(x => x.ProductId == productId, ct);

    private IQueryable<ProductWarning> Warnings(Guid shopId)
    {
        return _db.Set<ProductWarning>().AsNoTracking()
            .Where(x => x.ShopId == shopId);
    }

    public async Task<Product?> GetProductForWarningAsync(Guid productId, CancellationToken ct)
    {
        return await _db.Products.AsNoTracking().Include(p => p.Attributes).ThenInclude(a => a.Values).FirstOrDefaultAsync(x => x.Id == productId, ct);
    }

    public async Task<Guid?> GetOwnedShopIdAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await _db.Shops.AsNoTracking()
            .Where(x => x.OwnerUserId == ownerId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ProductWarning> Items, int Total)> GetHistoryAsync(Guid shopId, int page, int pageSize, CancellationToken cancellationToken, string? status = null)
    {
        var query = Warnings(shopId);
        query = status switch
        {
            "new" => query.Where(w => w.ResolvedAt == null && w.FirstEditedAt == null),
            "submitted" => query.Where(w => w.ResolvedAt == null && w.FirstEditedAt != null),
            "resolved" => query.Where(w => w.ResolvedAt != null),
            _ => query
        };
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<ProductWarning?> GetByIdAsync(Guid shopId, Guid warningId, CancellationToken cancellationToken)
    {
        return await Warnings(shopId).FirstOrDefaultAsync(x => x.Id == warningId, cancellationToken);
    }
}