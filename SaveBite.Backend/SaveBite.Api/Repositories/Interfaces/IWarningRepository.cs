using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Repositories.Interfaces;
public interface IWarningRepository
{
    Task<ProductContentRevision?> GetLatestRevisionForWarningAsync(Guid productId, CancellationToken ct);
    Task<Product?> GetProductForWarningAsync(Guid productId, CancellationToken ct);
    Task<Guid?> GetOwnedShopIdAsync(Guid ownerId, CancellationToken ct);
    Task<(IReadOnlyList<ProductWarning> Items, int Total)> GetHistoryAsync(Guid shopId, int page, int pageSize, CancellationToken ct, string? status = null);
    Task<ProductWarning?> GetByIdAsync(Guid shopId, Guid warningId, CancellationToken ct);
}
