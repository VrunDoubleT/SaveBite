using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IPlatformFeeConfigRepository
{
    Task<PlatformFeeConfig?> GetActiveConfigAsync(DateTime asOf, CancellationToken ct = default);
    Task<PlatformFeeConfig?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PlatformFeeConfig>> GetAllConfigsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PlatformFeeConfig>> GetPendingConfigsAsync(DateTime asOf, CancellationToken ct = default);
    Task<PlatformFeeConfig?> GetUpcomingConfigAsync(DateTime asOf, CancellationToken ct = default);
    Task AddConfigAsync(PlatformFeeConfig config, CancellationToken ct = default);
    void RemoveConfig(PlatformFeeConfig config);
    void AddAuditLog(AuditLog audit);
    Task SaveChangesAsync(CancellationToken ct = default);
}
