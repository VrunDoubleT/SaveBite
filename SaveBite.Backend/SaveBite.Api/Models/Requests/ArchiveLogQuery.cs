using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Exceptions;
namespace SaveBite.Backend.Models.Requests;

public sealed class ArchiveLogQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public string? ActorType { get; set; }
    public string? TargetType { get; set; }
    public string? Action { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? TargetId { get; set; }
    public Guid? CorrelationId { get; set; }
    public void Validate()
    {
        if (FromDate > ToDate) throw AppException.BadRequest("Start date must not follow end date.");
        if (!string.IsNullOrEmpty(ActorType) && ActorType is not ("User" or "System")) throw AppException.BadRequest("Invalid actor type.");
    }
    public bool Matches(AuditLog row)
    {
        var day = DateOnly.FromDateTime(row.CreatedAt.AddHours(7));
        if (FromDate is DateOnly from && day < from || ToDate is DateOnly to && day > to) return false;
        if (!string.IsNullOrEmpty(ActorType) && row.ActorType.ToString() != ActorType) return false;
        if (!string.IsNullOrEmpty(TargetType) && row.TargetType != TargetType) return false;
        if (!string.IsNullOrEmpty(Action) && row.Action != Action) return false;
        if (ActorUserId != null && row.ActorUserId != ActorUserId || TargetId != null && row.TargetId != TargetId || CorrelationId != null && row.CorrelationId != CorrelationId) return false;
        var search = Search?.Trim();
        return string.IsNullOrEmpty(search) || string.Join(" ", row.Id, row.ActorUserId, row.ActorType, row.Action, row.TargetType, row.TargetId, row.Reason, row.CorrelationId).Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
