using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Billing, fee accrual and all schedule changes share this PostgreSQL transaction lock.
public sealed class PlatformFeeConfigService(AppDbContext db, TimeProvider clock,
    IAdminAuditWriter audit, INotificationQueue notifications) : IPlatformFeeConfigService
{
    public const int MaxFutureSchedules = 5;
    private DateTime Now => Microseconds(clock.GetUtcNow().UtcDateTime);
    private static DateTime Microseconds(DateTime value) => new(value.Ticks / 10 * 10, DateTimeKind.Utc);

    public async Task<PlatformFeeConfigResponse> GetActiveConfigAsync(CancellationToken ct = default)
    {
        var now = Now;
        var config = await db.PlatformFeeConfigs.AsNoTracking().Include(c => c.CreatedByUser)
            .Where(c => c.LifecycleStatus == "Published" && c.EffectiveFrom <= now
                && (c.EffectiveTo == null || c.EffectiveTo > now))
            .OrderByDescending(c => c.EffectiveFrom).FirstOrDefaultAsync(ct)
            ?? throw AppException.NotFound("Active platform fee configuration not found.");
        return Response(config, now);
    }

    public async Task<IReadOnlyList<PlatformFeeConfigResponse>> GetAllConfigsAsync(CancellationToken ct = default)
    {
        var now = Now;
        var rows = await db.PlatformFeeConfigs.AsNoTracking().Include(c => c.CreatedByUser)
            .OrderByDescending(c => c.EffectiveFrom).ThenByDescending(c => c.CreatedAt).ToListAsync(ct);
        return rows.Select(c => Response(c, now)).ToList();
    }

    public async Task<PlatformFeeConfigResponse> UpdateConfigAsync(Guid adminId,
        UpdatePlatformFeeConfigRequest request, CancellationToken ct = default)
    {
        if (adminId == Guid.Empty) throw AppException.Forbidden();
        if (request.FeeRate < 0 || request.FeeRate > 100)
            throw AppException.BadRequest("Platform fee rate must be between 0% and 100%.");
        var reason = ValidateReason(request.Reason);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(ct);
        var now = Now;
        var rows = await db.PlatformFeeConfigs.Include(c => c.CreatedByUser).ToListAsync(ct);
        // Announce every due version before a mutation can close/replace the currently effective one.
        await ProcessDueAsync(rows, now, ct);
        var effectiveFrom = request.EffectiveFrom.HasValue
            ? Microseconds(request.EffectiveFrom.Value.ToUniversalTime()) : now;
        if (effectiveFrom < now.AddMinutes(-1))
            throw AppException.BadRequest("Effective date cannot be in the past.");
        if (effectiveFrom < now) effectiveFrom = now;
        var immediate = effectiveFrom <= now;
        var future = rows.Where(c => c.LifecycleStatus == "Published" && c.EffectiveFrom > now).ToList();
        PlatformFeeConfig? replaced = null;
        if (request.ReplacesConfigId.HasValue)
        {
            replaced = future.SingleOrDefault(c => c.Id == request.ReplacesConfigId.Value)
                ?? throw AppException.Conflict("This schedule is no longer upcoming. Refresh before making changes.");
            if (immediate) throw AppException.BadRequest("A replacement schedule must start in the future. Use Apply now for an immediate change.");
            if (request.CancelUpcomingScheduled) throw AppException.BadRequest("Replace one schedule or cancel all upcoming schedules, not both.");
        }
        if (!immediate && request.CancelUpcomingScheduled)
            throw AppException.BadRequest("Cancel upcoming schedules is only available when applying a rate immediately.");
        if (!immediate && future.Count >= MaxFutureSchedules && replaced == null)
            throw AppException.BadRequest($"A maximum of {MaxFutureSchedules} future fee schedules is allowed. Replace or cancel a schedule first.");
        if (rows.Any(c => c.LifecycleStatus == "Published" && c.Id != replaced?.Id && c.EffectiveFrom == effectiveFrom))
        {
            if (!immediate) throw AppException.Conflict("Another fee schedule already starts at this time.");
            // Rapid immediate changes still get disjoint nonempty intervals at PostgreSQL precision.
            effectiveFrom = rows.Where(c => c.LifecycleStatus == "Published" && c.EffectiveFrom <= now)
                .Max(c => c.EffectiveFrom).AddTicks(10);
        }
        if (replaced != null) Close(replaced, "Superseded", adminId, reason, now);
        if (request.CancelUpcomingScheduled)
            foreach (var item in future) Close(item, "Cancelled", adminId, reason, now);

        // Persist terminal decisions before inserting a replacement at the same start time.
        // This releases the partial unique index inside the SAME transaction.
        if (replaced != null || request.CancelUpcomingScheduled) await db.SaveChangesAsync(ct);
        var previous = rows.Where(c => c.LifecycleStatus == "Published" && c.EffectiveFrom < effectiveFrom)
            .OrderByDescending(c => c.EffectiveFrom).FirstOrDefault();
        var config = new PlatformFeeConfig
        {
            Id = Guid.NewGuid(), FeeRate = Math.Round(request.FeeRate / 100m, 6),
            EffectiveFrom = effectiveFrom, CreatedBy = adminId, CreatedAt = now, Reason = reason,
            ReplacesConfigId = replaced?.Id ?? (immediate ? previous?.Id : null)
        };
        db.PlatformFeeConfigs.Add(config);
        rows.Add(config);
        RebuildTimeline(rows);
        RecordAudit(adminId, config.Id, replaced == null ? "FEE_SCHEDULE_CREATED" : "FEE_SCHEDULE_REPLACED",
            new { config.FeeRate, config.EffectiveFrom, config.ReplacesConfigId, config.Reason, request.CancelUpcomingScheduled }, now);
        await db.SaveChangesAsync(ct);
        await ProcessDueAsync(rows, now, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Response(config, now);
    }

    public async Task CancelScheduledConfigAsync(Guid adminId, Guid configId, CancellationToken ct = default, string? reason = null)
    {
        if (adminId == Guid.Empty) throw AppException.Forbidden();
        var explanation = ValidateReason(reason);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(ct);
        var now = Now;
        var rows = await db.PlatformFeeConfigs.ToListAsync(ct);
        var target = rows.SingleOrDefault(c => c.Id == configId) ?? throw AppException.NotFound();
        if (target.LifecycleStatus != "Published" || target.EffectiveFrom <= now)
            throw AppException.Conflict("Only an upcoming schedule can be cancelled. Refresh the schedule list.");
        Close(target, "Cancelled", adminId, explanation, now);
        RebuildTimeline(rows);
        RecordAudit(adminId, configId, "FEE_SCHEDULE_CANCELLED",
            new { target.FeeRate, target.EffectiveFrom, Reason = explanation }, now);
        await ProcessDueAsync(rows, now, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ProcessEffectiveConfigsAsync(CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(ct);
        var rows = await db.PlatformFeeConfigs.ToListAsync(ct);
        await ProcessDueAsync(rows, Now, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task ProcessDueAsync(List<PlatformFeeConfig> rows, DateTime now, CancellationToken ct)
    {
        var timeline = rows.Where(c => c.LifecycleStatus == "Published").OrderBy(c => c.EffectiveFrom).ToList();
        foreach (var current in timeline.Where(c => c.EffectiveFrom <= now && c.ActivationProcessedAt == null))
        {
            var previous = timeline.LastOrDefault(c => c.EffectiveFrom < current.EffectiveFrom);
            if (previous != null && previous.ClosedAt == null)
            {
                previous.ClosedAt = current.EffectiveFrom;
                previous.ClosedBy = current.CreatedBy;
                previous.ClosureReason = $"Succeeded by fee version {current.Id}. {current.Reason}";
                if (previous.ClosureReason.Length > 500) previous.ClosureReason = previous.ClosureReason[..500];
            }
            var changed = previous == null || previous.FeeRate != current.FeeRate;
            var from = previous == null ? "not configured" : Percent(previous.FeeRate);
            var to = Percent(current.FeeRate);
            var when = current.EffectiveFrom.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " (UTC+7)";
            await notifications.EnqueueAsync(NotificationTarget.Admins(), new(
                NotificationType.PlatformFeeScheduleActivated, "Platform fee schedule is effective",
                $"Schedule {current.Id} became effective at {when}. Fee: {from} → {to}. "
                    + (changed ? "The commission rate changed." : "The commission rate is unchanged."),
                "/admin/platform-fees", $"fee-schedule:{current.Id}:activated:admin"), ct);
            if (changed)
            {
                // A catch-up run targets owners whose shops existed at this effective time.
                var owners = await db.Shops.AsNoTracking().Where(s => s.CreatedAt <= current.EffectiveFrom)
                    .Select(s => s.OwnerUserId).Distinct().ToListAsync(ct);
                foreach (var ownerId in owners)
                    await notifications.EnqueueAsync(NotificationTarget.User(ownerId), new(
                        NotificationType.PlatformFeeRateChanged, "Platform commission rate changed",
                        $"The platform commission changed from {from} to {to}, effective {when}. "
                            + "Orders completed from that time use this rate until the next fee schedule. Fees already recorded on earlier orders remain unchanged.",
                        "/store-owner/platform-fees", $"fee-schedule:{current.Id}:rate-changed"), ct);
            }
            current.ActivationProcessedAt = now;
        }
    }

    private static void RebuildTimeline(IEnumerable<PlatformFeeConfig> rows)
    {
        var timeline = rows.Where(c => c.LifecycleStatus == "Published").OrderBy(c => c.EffectiveFrom).ToArray();
        for (var index = 0; index < timeline.Length; index++)
            timeline[index].EffectiveTo = index + 1 < timeline.Length ? timeline[index + 1].EffectiveFrom : null;
    }
    private static void Close(PlatformFeeConfig config, string status, Guid adminId, string reason, DateTime now)
    {
        config.LifecycleStatus = status;
        config.ClosedAt = now;
        config.ClosedBy = adminId;
        config.ClosureReason = reason;
    }
    private Task LockAsync(CancellationToken ct) => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);
    private static string ValidateReason(string? value)
    {
        var reason = value?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length < 3 || reason.Length > 500)
            throw AppException.BadRequest("Provide a reason between 3 and 500 characters.");
        return reason;
    }
    private static string Percent(decimal rate) => (rate * 100m).ToString("0.####", CultureInfo.InvariantCulture) + "%";
    private void RecordAudit(Guid adminId, Guid id, string action, object values, DateTime now) => audit.Record(new AuditLog
    {
        ActorUserId = adminId, ActorType = AuditActorType.User, TargetType = "PlatformFeeConfig", TargetId = id, Action = action,
        NewValuesJson = JsonSerializer.Serialize(values), CreatedAt = now
    });
    private static PlatformFeeConfigResponse Response(PlatformFeeConfig c, DateTime now) => new(
        c.Id, c.FeeRate, c.FeeRate * 100m, c.EffectiveFrom, c.EffectiveTo, c.CreatedBy, c.CreatedAt)
    {
        Status = c.LifecycleStatus != "Published" ? c.LifecycleStatus
            : c.EffectiveFrom > now ? "Scheduled" : c.EffectiveTo <= now ? "Ended" : "Active",
        Reason = c.Reason, ReplacesConfigId = c.ReplacesConfigId, ClosedAt = c.ClosedAt, ClosedBy = c.ClosedBy,
        ClosureReason = c.ClosureReason, ActivationProcessedAt = c.ActivationProcessedAt,
        CreatedByName = c.CreatedByUser?.FullName, MaxFutureSchedules = MaxFutureSchedules
    };
}