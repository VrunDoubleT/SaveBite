using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Implementations;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Persistent, restart-safe scan. Stable outbox/delivery keys prevent each scan from mailing again.
public sealed class BusinessEmailReminderJob(IServiceScopeFactory scopes, TimeProvider clock, ILogger<BusinessEmailReminderJob> logger)
{
    [DisableConcurrentExecution(TimingConfiguration.Jobs.ConcurrencyLockTimeoutSeconds)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await RecoveryBatch.RunAsync(scopes, logger, "refund-email-reminder", db => db.Refunds.AsNoTracking()
            .Where(r => ((r.Status == RefundStatus.AwaitingCustomerDetails || r.Status == RefundStatus.PendingVerification)
                    && r.DestinationConfirmedAt == null && r.CustomerDetailsDeadlineAt > now
                    && (r.CreatedAt <= now.AddHours(-TimingConfiguration.EmailReminders.CustomerDetailsFirstHours)
                        || r.CustomerDetailsDeadlineAt <= now.AddHours(TimingConfiguration.EmailReminders.CustomerDetailsFinalLeadHours)))
                || ((r.Status == RefundStatus.AwaitingTransfer || r.Status == RefundStatus.Overdue)
                    && r.DestinationConfirmedAt != null && r.RequestedAmount > r.TransferredAmount
                    && r.ReviewDeadlineAt <= now.AddHours(TimingConfiguration.EmailReminders.ShopTransferFirstLeadHours))).Select(r => r.Id),
            (sp, id) => sp.GetRequiredService<BusinessEmailReminderService>().RefundAsync(id, ct), ct);
        await RecoveryBatch.RunAsync(scopes, logger, "invitation-email-reminder", db => db.StaffInvitations.AsNoTracking()
            .Where(i => i.Status == nameof(StaffInvitationStatus.Pending) && i.RespondedAt == null
                && i.InvitedAt <= now.AddDays(-TimingConfiguration.StaffInvitations.LifetimeDays).AddHours(TimingConfiguration.EmailReminders.InvitationLeadHours)
                && i.InvitedAt > now.AddDays(-TimingConfiguration.StaffInvitations.LifetimeDays)).Select(i => i.Id),
            (sp, id) => sp.GetRequiredService<BusinessEmailReminderService>().InvitationAsync(id, ct), ct);
        await RecoveryBatch.RunAsync(scopes, logger, "fee-email-reminder", db => db.PlatformFeeStatements.AsNoTracking()
            .Where(f => (f.Status == PlatformFeeStatementStatus.Issued || f.Status == PlatformFeeStatementStatus.Overdue)
                && f.PaidAt == null && f.PaymentPauseReason == null && f.TotalFeeAmount > 0 && f.DueAt <= now.AddHours(TimingConfiguration.PlatformFees.DueReminderLeadHours)).Select(f => f.Id),
            (sp, id) => sp.GetRequiredService<BusinessEmailReminderService>().FeeAsync(id, ct), ct);
    }
}
