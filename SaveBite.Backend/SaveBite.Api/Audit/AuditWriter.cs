using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Audit;

public class AuditWriter(AppDbContext db, IAuditContext ctx) : IAuditWriter
{

    public void Record(string targetType, Guid targetId, string action,
                       object? oldValues = null, object? newValues = null,
                       string? reason = null)
    {
        if (!ctx.Enabled) return;
        if (targetId == Guid.Empty || string.IsNullOrWhiteSpace(targetType) || targetType.Length > 100)
            throw new ArgumentException("A valid audit target is required.");
        var formattedAction = AuditActionTypes.Format(targetType, action);
        if (string.IsNullOrWhiteSpace(formattedAction) || formattedAction.Length > 100 || (reason ?? ctx.Reason)?.Length > 1000)
            throw new ArgumentException("Audit action or reason is invalid.");

        db.Set<AuditLog>().Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = ctx.ActorUserId,
            ActorType = ctx.ActorType,
            Action = formattedAction,
            TargetType = targetType,
            TargetId = targetId,
            OldValuesJson = AuditJson.SerializeSafe(oldValues),
            NewValuesJson = AuditJson.SerializeSafe(newValues),
            Reason = reason ?? ctx.Reason,
            CorrelationId = ctx.CorrelationId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
