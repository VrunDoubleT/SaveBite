using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Repositories.Interfaces;
public interface IWarningRepository
{
    Task<ProductContentRevision?> GetLatestRevisionForWarningAsync(Guid productId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<Product?> GetProductForWarningAsync(Guid productId, CancellationToken ct);
    Task<Guid?> GetOwnedShopIdAsync(Guid ownerId, CancellationToken ct);
    Task<(IReadOnlyList<AuditLog> Items, int Total)> GetHistoryAsync(Guid shopId, int page, int pageSize, CancellationToken ct);
    Task<AuditLog?> GetByIdAsync(Guid shopId, Guid warningId, CancellationToken ct);
}
