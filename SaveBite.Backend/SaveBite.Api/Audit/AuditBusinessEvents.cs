using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Audit;

// These histories are explicitly written by business commands and jobs. Reuse their
// decision, actor and reason instead of inventing another CRUD record for the history table.
public static class AuditBusinessEvents
{
    public static AuditLog? FromHistory(EntityEntry entry, IAuditContext context)
    {
        if (entry.Entity is Payment payment) return PaymentResult(entry, payment, context);
        if (entry.State != EntityState.Added) return null;
        return entry.Entity switch
        {
            OrderStatusHistory h => Create(nameof(Order), h.OrderId,
                h.FromStatus == null ? "CREATED" : $"STATUS_{h.ToStatus}", h.FromStatus, h.ToStatus,
                h.ChangedBy, h.Note, h.CreatedAt, context),
            RefundStatusHistory h => Create(nameof(Refund), h.RefundId,
                string.IsNullOrWhiteSpace(h.Decision) ? $"STATUS_{h.ToStatus}" : h.Decision,
                h.FromStatus, h.ToStatus, h.ActorId, h.Note, h.CreatedAt, context),
            FlashDealStatusHistory h => Create(nameof(FlashDeal), h.FlashDealId,
                h.FromStatus == null ? "CREATED" : $"STATUS_{h.ToStatus}", h.FromStatus, h.ToStatus,
                h.ChangedByUserId, h.Note, h.CreatedAt, context),
            ShopApplicationReviewLog h when h.FromStatus != null => Create(nameof(ShopApplication), h.ApplicationId,
                h.ToStatus.ToString(), h.FromStatus, h.ToStatus, h.AdminId ?? context.ActorUserId, h.Note, h.CreatedAt, context),
            UserRoleChangeLog h => Create(nameof(User), h.UserId, "ROLE_CHANGED", h.PreviousRole, h.NewRole,
                context.ActorUserId, h.Reason, h.CreatedAt, context),
            TrustProgressHistory h when h.ScoreDelta != 0 || h.ValueDelta != 0 => TrustChange(h, context),
            UserTrustLevelHistory h => Create("UserTrustLevel", h.UserId, "LEVEL_CHANGED", h.PreviousTrustLevelId,
                h.NewTrustLevelId, null, h.Reason.ToString(), h.CreatedAt, context),
            _ => null
        };
    }

    private static AuditLog? PaymentResult(EntityEntry entry, Payment payment, IAuditContext context)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified)) return null;
        var statusChanged = entry.State == EntityState.Added ||
            !Equals(entry.Property(nameof(Payment.Status)).OriginalValue, payment.Status);
        var moneyChanged = entry.State == EntityState.Modified &&
            !Equals(entry.Property(nameof(Payment.CollectedAmount)).OriginalValue, payment.CollectedAmount);
        if ((!statusChanged && !moneyChanged) || (payment.Status == PaymentStatus.Pending && !moneyChanged)) return null;
        var action = payment.Status == PaymentStatus.Succeeded ? "VERIFIED" : payment.Status.ToString();
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            TargetType = nameof(Payment),
            TargetId = payment.Id,
            Action = AuditActionTypes.Format(nameof(Payment), action),
            ActorUserId = context.ActorUserId,
            ActorType = context.ActorType,
            CorrelationId = context.CorrelationId,
            Reason = context.Reason,
            CreatedAt = DateTime.UtcNow,
            OldValuesJson = entry.State == EntityState.Added ? null : JsonSerializer.Serialize(new
            {
                Status = entry.Property(nameof(Payment.Status)).OriginalValue?.ToString(),
                CollectedAmount = entry.Property(nameof(Payment.CollectedAmount)).OriginalValue
            }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                payment.OrderId,
                Status = payment.Status.ToString(),
                Type = payment.PaymentType.ToString(),
                Method = payment.Method.ToString(),
                payment.Amount,
                payment.CollectedAmount,
                payment.GatewayOrderCode,
                payment.GatewayTransactionId,
                payment.PaidAt
            })
        };
        return log;
    }

    private static AuditLog TrustChange(TrustProgressHistory history, IAuditContext context)
    {
        var log = Create("UserTrustScore", history.UserId, history.EventType.ToString(), null,
            history.ScoreAfter, null, history.Reason, history.CreatedAt, context);
        log.OldValuesJson = JsonSerializer.Serialize(new { history.ScoreBefore, history.ValueBefore });
        log.NewValuesJson = JsonSerializer.Serialize(new
        {
            history.ScoreAfter,
            history.ScoreDelta,
            history.ValueAfter,
            history.ValueDelta,
            history.OrderId,
            history.RuleId
        });
        return log;
    }

    private static AuditLog Create(string type, Guid id, string action, object? before, object after,
        Guid? actor, string? reason, DateTime occurredAt, IAuditContext context) => new()
        {
            Id = Guid.NewGuid(),
            TargetType = type,
            TargetId = id,
            Action = AuditActionTypes.Format(type, action),
            ActorUserId = actor,
            ActorType = actor.HasValue ? AuditActorType.User : AuditActorType.System,
            OldValuesJson = before == null ? null : JsonSerializer.Serialize(new { Status = before.ToString() }),
            NewValuesJson = JsonSerializer.Serialize(new { Status = after.ToString() }),
            Reason = reason ?? context.Reason,
            CorrelationId = context.CorrelationId,
            CreatedAt = occurredAt == default ? DateTime.UtcNow : occurredAt
        };
}
