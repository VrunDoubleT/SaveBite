using System.Text.Json;
using System.Text.Json.Serialization;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Audit;

public class AuditWriter(AppDbContext db, IAuditContext ctx) : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public void Record(string targetType, Guid targetId, string action,
                       object? oldValues = null, object? newValues = null,
                       string? reason = null)
    {
        if (!ctx.Enabled) return;

        db.Set<AuditLog>().Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = ctx.ActorUserId,
            ActorType = ctx.ActorType,
            Action = AuditActionTypes.Format(targetType, action),
            TargetType = targetType,
            TargetId = targetId,
            OldValuesJson = oldValues is null
                ? null : JsonSerializer.Serialize(oldValues, JsonOpts),
            NewValuesJson = newValues is null
                ? null : JsonSerializer.Serialize(newValues, JsonOpts),
            Reason = reason ?? ctx.Reason,
            CorrelationId = ctx.CorrelationId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
