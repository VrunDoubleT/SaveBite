using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class AuditLogService : IAuditLogService
{
    // audit_logs has no outcome column: entries are only written in the same transaction as a committed action,
    // so every stored entry is, by construction, a successful one.
    private const string SucceededOutcome = "Succeeded";

    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<AuditLogPageResponse> GetAuditLogsAsync(
        GetAuditLogsRequest request,
        CancellationToken ct = default)
    {
        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value > request.ToDate.Value)
        {
            throw AppException.BadRequest("FromDate cannot be later than ToDate.");
        }

        // Day boundaries follow the Vietnam calendar (same as the dashboards); the upper bound is exclusive.
        DateTime? fromUtc = request.FromDate.HasValue
            ? VietnamTime.StartOfDayUtc(request.FromDate.Value)
            : null;

        DateTime? toUtcExclusive = request.ToDate.HasValue
            ? VietnamTime.StartOfDayUtc(request.ToDate.Value.AddDays(1))
            : null;

        var paged = await _repository.GetPagedLogsAsync(request, fromUtc, toUtcExclusive, ct);
        var (actions, targetTypes) = await _repository.GetDistinctFiltersAsync(ct);

        var items = paged.Items.Select(MapToResponse).ToList();

        return new AuditLogPageResponse(
            PagedResult<AuditLogItemResponse>.Create(items, paged.Page, paged.PageSize, paged.TotalItems),
            new AuditLogFilterOptionsResponse(actions, targetTypes)
        );
    }

    public async Task<AuditLogItemResponse> GetAuditLogByIdAsync(Guid id, CancellationToken ct = default)
    {
        var log = await _repository.GetByIdAsync(id, ct)
                  ?? throw AppException.NotFound("Audit log entry not found.");

        return MapToResponse(log);
    }

    // BR-163: Audit entries record actor, action, target, timestamp, and outcome while excluding secrets.
    // Redaction is recursive and also protects entries that were stored before secrets were filtered on write.
    private static AuditLogItemResponse MapToResponse(AuditLog log)
    {
        var actorName = log.ActorType == AuditActorType.System
            ? "System"
            : log.ActorUser?.FullName ?? "Administrator";

        return new AuditLogItemResponse(
            log.Id,
            log.ActorUserId,
            log.ActorType.ToString(),
            actorName,
            log.ActorUser?.Email,
            log.Action,
            log.TargetType,
            log.TargetId,
            AuditPayloadSanitizer.ToSafeDictionary(log.OldValuesJson),
            AuditPayloadSanitizer.ToSafeDictionary(log.NewValuesJson),
            log.Reason,
            log.CorrelationId,
            SucceededOutcome,
            log.CreatedAt
        );
    }
}
