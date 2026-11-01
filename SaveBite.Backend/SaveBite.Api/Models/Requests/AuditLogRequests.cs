using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Requests;

/// <summary>UC-147: query for the administrative audit log list.</summary>
public sealed record GetAuditLogsRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    AuditActorType? ActorType = null,
    string? TargetType = null,
    string? Action = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
);