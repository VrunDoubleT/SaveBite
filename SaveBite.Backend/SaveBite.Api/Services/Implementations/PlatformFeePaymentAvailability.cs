using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// All callers serialize billing/configuration with advisory lock 728194602.
public sealed class PlatformFeePaymentAvailability(AppDbContext db, IConfiguration configuration,
    INotificationQueue notifications, TimeProvider clock, IShopStatusEventService shopEvents)
{
    public const string MissingConfiguration = "PayOsNotVerified";
    public const string InvalidConfiguration = "PayOsCredentialsInvalid";

    public async Task<bool> IsReadyAsync(CancellationToken ct)
    {
        if (configuration["Checkout:PaymentProvider"] == "Simulated") return true;
        return await db.PlatformPayOsConfigs.AnyAsync(c => c.VerifiedAt != null && !c.IsInvalid, ct);
    }

    public async Task PauseAsync(PlatformFeeStatement statement, string reason, CancellationToken ct)
    {
        if (statement.Status == PlatformFeeStatementStatus.Paid || statement.TotalFeeAmount <= 0) return;
        if (statement.PaymentPauseReason != null) return;
        var now = clock.GetUtcNow().UtcDateTime;
        statement.PaymentPauseReason = reason;
        statement.PaymentPausedAt = now;
        statement.DueAt = null;
        statement.Status = PlatformFeeStatementStatus.Issued;
        statement.UpdatedAt = now;
        db.PlatformFeeStatementHistories.Add(History(statement, StatementChangeType.PaymentPaused,
            "Payment deadline paused because platform payOS configuration is unavailable or unverified.", now));
        await NotifyPausedAsync(statement, ct);
    }

    public async Task NotifyPausedAsync(PlatformFeeStatement statement, CancellationToken ct)
    {
        if (statement.PaymentPauseReason == null || statement.Status == PlatformFeeStatementStatus.Paid) return;
        await notifications.EnqueueAsync(NotificationTarget.ShopOwner(statement.ShopId), new(
            NotificationType.PaymentFailed, "Platform fee payment temporarily unavailable",
            "Your statement is available, but payment is temporarily unavailable due to the platform's payment configuration. The payment deadline is paused. You will receive a new 7-day deadline when payment is restored.",
            NotificationActionUrls.StoreOwnerPlatformFee(statement.Id),
            $"platform-fee:{statement.Id}:paused:{statement.PaymentPausedAt?.Ticks / 10 ?? 0}"), ct);
        var revision = await db.PlatformPayOsConfigs.Select(c => (long?)c.Revision).SingleOrDefaultAsync(ct) ?? 0;
        await notifications.EnqueueAsync(NotificationTarget.Admins(), new(
            NotificationType.PaymentFailed, "Action required: platform payOS configuration",
            "Platform fee payments are paused because payOS credentials are missing, unverified or rejected. Verify and save the platform payOS configuration to restore payment deadlines.",
            NotificationActionUrls.AdminPlatformFees(), $"platform-fee-config-unavailable:{revision}"), ct);
    }

    public async Task PauseOutstandingAsync(string reason, CancellationToken ct)
    {
        var statements = await db.PlatformFeeStatements.Where(s => s.TotalFeeAmount > 0
            && (s.Status == PlatformFeeStatementStatus.Issued || s.Status == PlatformFeeStatementStatus.Overdue))
            .ToListAsync(ct);
        foreach (var statement in statements) await PauseAsync(statement, reason, ct);
        await db.SaveChangesAsync(ct);
        await ResolveFeeSuspensionsAsync(ct);
    }

    private async Task ResolveFeeSuspensionsAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var shops = await db.Shops.Where(s => s.SuspendedForPlatformFees).ToListAsync(ct);
        foreach (var shop in shops)
        {
            if (await db.PlatformFeeStatements.AnyAsync(s => s.ShopId == shop.Id && s.TotalFeeAmount > 0
                && s.PaymentPauseReason == null && s.Status != PlatformFeeStatementStatus.Paid
                && s.DueAt < now, ct)) continue;
            shop.SuspendedForPlatformFees = false;
            shop.SuspendedForOverdueRefunds = await db.Refunds.AnyAsync(r => r.Order.ShopId == shop.Id
                && r.RefundType == RefundType.ShopCancellation && r.DestinationConfirmedAt != null
                && r.RequestedAmount > r.TransferredAmount && (r.Status == RefundStatus.Overdue
                    || r.Status == RefundStatus.AwaitingTransfer && r.ReviewDeadlineAt <= now), ct);
            ShopSuspensionRules.ReactivateIfResolved(shop);
            shop.UpdatedAt = now;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task ResumeAsync(long revision, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);
        var config = await db.PlatformPayOsConfigs.SingleOrDefaultAsync(ct);
        // Redelivered/obsolete events cannot extend deadlines or reopen a new outage.
        if (config == null || config.Revision != revision || config.VerifiedAt == null || config.IsInvalid) return;
        var now = clock.GetUtcNow().UtcDateTime;
        var statements = await db.PlatformFeeStatements.Where(s => s.PaymentPauseReason != null
            && s.TotalFeeAmount > 0 && s.Status != PlatformFeeStatementStatus.Paid).ToListAsync(ct);
        foreach (var statement in statements)
        {
            statement.DueAt = now.AddDays(TimingConfiguration.PlatformFees.StatementPaymentDays);
            statement.PaymentPauseReason = null;
            statement.PaymentPausedAt = null;
            statement.PaymentResumeRevision = revision;
            statement.Status = PlatformFeeStatementStatus.Issued;
            statement.UpdatedAt = now;
            db.PlatformFeeStatementHistories.Add(History(statement, StatementChangeType.PaymentResumed,
                $"Payment restored with verified payOS configuration revision {revision}. A new 7-day deadline was assigned.", now));
            await notifications.EnqueueAsync(NotificationTarget.ShopOwner(statement.ShopId), new(
                NotificationType.PlatformFeeStatementIssued, "Platform fee payment is now available",
                $"You can now pay your platform fee statement. Your new payment deadline is {statement.DueAt:yyyy-MM-dd HH:mm} UTC (7 days from restoration).",
                NotificationActionUrls.StoreOwnerPlatformFee(statement.Id), $"platform-fee:{statement.Id}:resumed:{revision}"), ct);
        }
        await db.SaveChangesAsync(ct);
        await ResolveFeeSuspensionsAsync(ct);
        await tx.CommitAsync(ct);
        foreach (var shopId in statements.Select(s => s.ShopId).Distinct())
        {
            var status = await db.Shops.Where(s => s.Id == shopId).Select(s => s.Status).SingleAsync(ct);
            await shopEvents.NotifyChangedAsync(shopId, status.ToString(), ct);
        }
    }

    private static PlatformFeeStatementHistory History(PlatformFeeStatement s, StatementChangeType type, string reason, DateTime now)
        => new() { Id = Guid.NewGuid(), StatementId = s.Id, ChangeType = type,
            PreviousAmount = s.TotalFeeAmount, NewAmount = s.TotalFeeAmount, ChangeAmount = 0,
            Reason = reason, CreatedAt = now };
}
