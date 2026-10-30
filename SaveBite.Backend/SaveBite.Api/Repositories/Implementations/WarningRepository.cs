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

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    private IQueryable<AuditLog> Warnings(Guid shopId)
    {
        return _db.Set<AuditLog>().AsNoTracking()
            .Where(x =>
                x.TargetType == "Shop" &&
                x.TargetId == shopId &&
                x.Action == "SHOP_WARNING_ISSUED");
    }

    public async Task<Product?> GetProductForWarningAsync(Guid productId, CancellationToken ct)
    {
        return await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productId, ct);
    }
    
    public async Task<Guid?> GetOwnedShopIdAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await _db.Shops.AsNoTracking()
            .Where(x => x.OwnerUserId == ownerId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> GetHistoryAsync(Guid shopId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = Warnings(shopId);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<AuditLog?> GetByIdAsync(Guid shopId, Guid warningId, CancellationToken cancellationToken)
    {
        return await Warnings(shopId).FirstOrDefaultAsync(x => x.Id == warningId, cancellationToken);
    }
}