using SaveBite.Backend.Audit;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

// Scoped writer shares the caller's DbContext. It never starts a transaction or saves independently.
public sealed class AdminAuditWriter(AppDbContext db, TimeProvider clock, IAuditContext context) : IAdminAuditWriter
{
    public AuditLog Record(AuditLog entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.ActorUserId is null || entry.ActorUserId == Guid.Empty || entry.TargetId == Guid.Empty)
            throw new ArgumentException("An administrator identity and target are required.");
        if (string.IsNullOrWhiteSpace(entry.Action) || entry.Action.Length > 100
            || string.IsNullOrWhiteSpace(entry.TargetType) || entry.TargetType.Length > 100
            || entry.Reason?.Length > 1000)
            throw new ArgumentException("Audit action, target type or reason is invalid.");
        // Authorization belongs to the admin command; never accept actor IDs directly from client payloads.
        entry.ActorType = AuditActorType.User;
        entry.CorrelationId ??= context.CorrelationId;
        if (entry.Id == Guid.Empty) entry.Id = Guid.NewGuid();
        if (entry.CreatedAt == default) entry.CreatedAt = clock.GetUtcNow().UtcDateTime;
        entry.OldValuesJson = AuditPayloadSanitizer.SanitizeJson(entry.OldValuesJson);
        entry.NewValuesJson = AuditPayloadSanitizer.SanitizeJson(entry.NewValuesJson);
        db.AuditLogs.Add(entry);
        return entry;
    }
}
