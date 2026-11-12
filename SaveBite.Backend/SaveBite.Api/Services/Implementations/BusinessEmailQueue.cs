using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
namespace SaveBite.Backend.Services.Implementations;

// Staged in the business transaction; delivery uses the existing outbox, RabbitMQ and SMTP deduplication.
public sealed class BusinessEmailQueue(AppDbContext db, TimeProvider clock, IConfiguration configuration)
{
    public string Link(string path) => (configuration["Checkout:FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/') + path;
    public static string DeadlineText(DateTime at) => at.AddHours(7).ToString("dd/MM/yyyy HH:mm") + " (GMT+7)";

    public async Task StageAsync(string key, string to, string subject, string body, string kind, Guid subjectId,
        DateTime? deadline, DateTime? expires, CancellationToken ct, decimal? expectedOutstandingAmount = null)
    {
        if (db.Database.CurrentTransaction == null) throw new InvalidOperationException("Email staging requires a business transaction.");
        if (string.IsNullOrWhiteSpace(to)) return;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({key}))", ct);
        if (db.Set<OrderEventOutbox>().Local.Any(e => e.EventKey == key)
            || await db.Set<OrderEventOutbox>().AnyAsync(e => e.EventKey == key, ct)
            || await db.Set<EmailDelivery>().AnyAsync(e => e.DeliveryKey == key, ct)) return;
        db.Add(new OrderEventOutbox
        {
            Id = Guid.NewGuid(), EventKey = key, RoutingKey = RabbitMqRoutingKeys.EmailRoutingKey,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            // Do not use OrderId: that field currently identifies pickup reminder delivery receipts.
            PayloadJson = JsonSerializer.Serialize(new EmailNotificationEvent
            {
                DeliveryKey = key, To = to, Subject = subject, Body = body, IsHtml = false, ExpiresAt = expires,
                BusinessKind = kind, BusinessSubjectId = subjectId, ExpectedDeadline = deadline, ExpectedOutstandingAmount = expectedOutstandingAmount
            })
        });
    }

    public async Task StageOfflineLockAsync(Guid userId, Guid penaltyId, CancellationToken ct)
    {
        var address = await db.Users.Where(u => u.Id == userId).Select(u => u.Email).SingleAsync(ct);
        await StageAsync($"email:permanent-offline-lock:{userId}", address, "SaveBite: pay at shop permanently restricted",
            $"Repeated missed pickups have permanently disabled payment at the shop for your account. Full online payment is required. Completing recovery orders does not remove this permanent restriction. Penalty reference: {penaltyId}.\n{Link("/account/trust-score/penalties")}",
            "offline-locked", userId, null, null, ct);
    }

    // Recheck at delivery too: a queue delay must not remind a completed task or an obsolete deadline.
    public async Task<bool> StillRelevantAsync(EmailNotificationEvent email, CancellationToken ct)
    {
        if (email.BusinessKind == null) return true;
        if (email.BusinessSubjectId is not Guid id) return false;
        var now = clock.GetUtcNow().UtcDateTime;
        if (email.ExpiresAt <= now) return false;
        switch (email.BusinessKind)
        {
            case "refund-details":
            case "refund-transfer":
                var refund = await db.Refunds.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
                if (refund == null || (email.ExpectedOutstandingAmount.HasValue && email.ExpectedOutstandingAmount != refund.RequestedAmount - refund.TransferredAmount)) return false;
                return email.BusinessKind == "refund-details"
                    ? RefundRules.SameDeadline(refund.CustomerDetailsDeadlineAt, email.ExpectedDeadline ?? default) && EmailReminderRules.CustomerDetails(refund, now) != null
                    : RefundRules.SameDeadline(refund.ReviewDeadlineAt, email.ExpectedDeadline ?? default) && EmailReminderRules.ShopTransfer(refund, now) != null;
            case "invitation":
                var invitation = await db.StaffInvitations.AsNoTracking().SingleOrDefaultAsync(i => i.Id == id, ct);
                return invitation != null && EmailReminderRules.PendingInvitation(invitation, now)
                    && RefundRules.SameDeadline(EmailReminderRules.InvitationDeadline(invitation), email.ExpectedDeadline ?? default);
            case "fee-due":
            case "fee-overdue":
                var fee = await db.PlatformFeeStatements.AsNoTracking().SingleOrDefaultAsync(f => f.Id == id, ct);
                return fee != null && (!email.ExpectedOutstandingAmount.HasValue || email.ExpectedOutstandingAmount == fee.TotalFeeAmount) && EmailReminderRules.UnpaidFee(fee) && RefundRules.SameDeadline(fee.DueAt, email.ExpectedDeadline ?? default)
                    && (email.BusinessKind == "fee-overdue" ? fee.DueAt <= now : fee.DueAt > now);
            case "account-suspended": return await db.Users.AnyAsync(u => u.Id == id && u.Status == UserStatus.Suspended, ct);
            case "customer-suspended": return await db.Users.AnyAsync(u => u.Id == id && u.CustomerStatus == CustomerStatus.Suspended, ct);
            case "offline-locked": return await db.UserTrustScores.AnyAsync(s => s.UserId == id && s.IsOfflineLockedPermanently, ct);
            default: return false;
        }
    }
}
