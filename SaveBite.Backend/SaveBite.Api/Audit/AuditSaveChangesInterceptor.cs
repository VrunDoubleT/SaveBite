using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Audit;

public class AuditSaveChangesInterceptor(IAuditContext ctx) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData e,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (e.Context is not null && ctx.Enabled)
        {
            AddAuditRecords(e.Context);
        }

        return base.SavingChangesAsync(e, result, ct);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData e,
        InterceptionResult<int> result)
    {
        if (e.Context is not null && ctx.Enabled)
        {
            AddAuditRecords(e.Context);
        }

        return base.SavingChanges(e, result);
    }

    private void AddAuditRecords(DbContext db)
    {
        db.ChangeTracker.DetectChanges();

        var entries = db.ChangeTracker.Entries()
            .Where(x => x.State is EntityState.Added
                                or EntityState.Modified
                                or EntityState.Deleted)
            .Where(x => x.Entity is not AuditLog)
            .Where(x => x.Entity.GetType().GetCustomAttribute<NoAuditAttribute>() is null)
            .ToList(); // BẮT BUỘC ToList(): ta sắp Add AuditLog vào ChangeTracker

        foreach (var entry in entries)
        {
            var pkProps = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
            if (pkProps.Count != 1) continue;

            var pkProp = pkProps[0];
            if (pkProp.CurrentValue is not Guid targetId) continue;

            if (entry.State == EntityState.Added && targetId == Guid.Empty)
            {
                targetId = Guid.NewGuid();
                pkProp.CurrentValue = targetId;
            }

            var (action, oldJson, newJson) = Build(entry);
            if (action is null) continue;

            var typeName = entry.Metadata.ClrType.Name;

            db.Set<AuditLog>().Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = ctx.ActorUserId,
                ActorType = ctx.ActorType,
                Action = AuditActionTypes.Format(typeName, action),
                TargetType = typeName,
                TargetId = targetId,
                OldValuesJson = oldJson,
                NewValuesJson = newJson,
                Reason = ctx.Reason,
                CorrelationId = ctx.CorrelationId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static (string? action, string? oldJson, string? newJson) Build(EntityEntry entry)
    {
        var props = entry.Properties
            .Where(p => p.Metadata.PropertyInfo?.IsDefined(typeof(AuditIgnoreAttribute)) != true)
            .ToList();

        switch (entry.State)
        {
            case EntityState.Added:
                return (AuditActionTypes.Create, null, Serialize(props, p => p.CurrentValue));

            case EntityState.Deleted:
                return (AuditActionTypes.Delete, Serialize(props, p => p.OriginalValue), null);

            case EntityState.Modified:
                var changed = props
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                    .ToList();
                if (changed.Count == 0) return (null, null, null);

                var isSoftDelete = changed.Any(p =>
                    (p.Metadata.Name == "IsDeleted" && p.CurrentValue is true) ||
                    (p.Metadata.Name == "DeletedAt" && p.CurrentValue is not null && p.OriginalValue is null));

                return (isSoftDelete ? AuditActionTypes.Delete : AuditActionTypes.Update,
                    Serialize(changed, p => p.OriginalValue),
                    Serialize(changed, p => p.CurrentValue));
        }

        return (null, null, null);
    }

    private static string Serialize(IEnumerable<PropertyEntry> props, Func<PropertyEntry, object?> pick)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var p in props)
        {
            dict[p.Metadata.Name] = pick(p);
        }

        return JsonSerializer.Serialize(dict, JsonOpts);
    }
}
