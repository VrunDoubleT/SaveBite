using SaveBite.Backend.Models.Common;

namespace SaveBite.Backend.Models.Responses;

// UC-147 & BR-163: Record actor, action, target, timestamp, and outcome while excluding secrets.
public sealed record AuditLogItemResponse(
    Guid Id,
    Guid? ActorUserId,
    string ActorType,
    string ActorName,
    string? ActorEmail,
    string Action,
    string TargetType,
    Guid TargetId,
    Dictionary<string, object?>? OldValues,
    Dictionary<string, object?>? NewValues,
    string? Reason,
    Guid? CorrelationId,
    string Outcome,
    DateTime CreatedAt
);

public sealed record AuditLogFilterOptionsResponse(
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> TargetTypes
);

public sealed record AuditLogPageResponse(
    PagedResult<AuditLogItemResponse> Page,
    AuditLogFilterOptionsResponse FilterOptions
);