using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task<PagedResult<AuditLog>> GetPagedLogsAsync(
        GetAuditLogsRequest request,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default);

    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<string> Actions, IReadOnlyList<string> TargetTypes)> GetDistinctFiltersAsync(
        CancellationToken ct = default);
}