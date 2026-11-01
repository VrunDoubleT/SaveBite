using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IAuditLogService
{
    Task<AuditLogPageResponse> GetAuditLogsAsync(GetAuditLogsRequest request, CancellationToken ct = default);
    Task<AuditLogItemResponse> GetAuditLogByIdAsync(Guid id, CancellationToken ct = default);
}