using SaveBite.Backend.Configurations;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Enums;
namespace SaveBite.Backend.Services.Implementations;

public sealed class BusinessEmailReminderService(AppDbContext db, BusinessEmailQueue emails, TimeProvider clock)
{
    public Task RefundAsync(Guid id, CancellationToken ct) => NotificationTransaction.RunAsync(db, async () =>
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM refunds WHERE id = {id} FOR UPDATE", ct);
        var refund = await db.Refunds.Include(r => r.Order).ThenInclude(o => o.Customer)
            .Include(r => r.Order).ThenInclude(o => o.Shop).ThenInclude(s => s.Owner).SingleOrDefaultAsync(r => r.Id == id, ct);
        if (refund == null) return;
        var now = clock.GetUtcNow().UtcDateTime;
        if (EmailReminderRules.CustomerDetails(refund, now) is { } customer)
        {
            var deadline = refund.CustomerDetailsDeadlineAt!.Value;
            await emails.StageAsync($"email:refund-details:{id}:{deadline.Ticks}:{customer.Stage}", refund.Order.Customer.Email,
                "SaveBite: complete your refund destination",
                $"Your refund for order {refund.Order.OrderCode} is waiting for your receiving details and email confirmation. Complete both by {BusinessEmailQueue.DeadlineText(deadline)}. If your verification code expired, request a new code on the refund page.\n{emails.Link($"/account/refunds/{id}")}",
                "refund-details", id, deadline, customer.ExpiresAt, ct);
        }
        if (EmailReminderRules.ShopTransfer(refund, now) is { } shop)
        {
            var deadline = refund.ReviewDeadlineAt!.Value;
            await emails.StageAsync($"email:refund-transfer:{id}:{deadline.Ticks}:{shop.Stage}", refund.Order.Shop.Owner.Email,
                shop.Stage == "overdue" ? "SaveBite: mandatory refund is overdue" : "SaveBite: refund transfer deadline approaching",
                $"The refund for order {refund.Order.OrderCode} at {refund.Order.Shop.Name} still has {refund.RequestedAmount - refund.TransferredAmount:N0} VND unpaid. Processing deadline: {BusinessEmailQueue.DeadlineText(deadline)}. Complete the transfer and record its reference in SaveBite.\n{emails.Link($"/store-owner/refunds/{id}")}",
                "refund-transfer", id, deadline, shop.ExpiresAt, ct, refund.RequestedAmount - refund.TransferredAmount);
        }
        await db.SaveChangesAsync(ct);
    }, ct);

    public Task InvitationAsync(Guid id, CancellationToken ct) => NotificationTransaction.RunAsync(db, async () =>
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM staff_invitations WHERE id = {id} FOR UPDATE", ct);
        var invitation = await db.StaffInvitations.Include(i => i.InvitedUser).Include(i => i.Shop).SingleOrDefaultAsync(i => i.Id == id, ct);
        if (invitation == null || !EmailReminderRules.PendingInvitation(invitation, clock.GetUtcNow().UtcDateTime)) return;
        var deadline = EmailReminderRules.InvitationDeadline(invitation);
        await emails.StageAsync($"email:staff-invitation:{id}:{deadline.Ticks}:due-24h", invitation.InvitedUser.Email,
            "SaveBite: your staff invitation expires soon",
            $"You have not responded to the staff invitation from {invitation.Shop.Name}. Accept or decline it before {BusinessEmailQueue.DeadlineText(deadline)}.\n{emails.Link("/account/staff-invitations")}",
            "invitation", id, deadline, deadline, ct);
        await db.SaveChangesAsync(ct);
    }, ct);

    public Task FeeAsync(Guid id, CancellationToken ct) => NotificationTransaction.RunAsync(db, async () =>
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM platform_fee_statements WHERE id = {id} FOR UPDATE", ct);
        var fee = await db.PlatformFeeStatements.Include(f => f.Shop).ThenInclude(s => s.Owner).SingleOrDefaultAsync(f => f.Id == id, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        if (fee == null || !EmailReminderRules.UnpaidFee(fee) || fee.DueAt > now.AddHours(TimingConfiguration.PlatformFees.DueReminderLeadHours)) return;
        var deadline = fee.DueAt!.Value;
        var overdue = deadline <= now;
        await emails.StageAsync($"email:platform-fee:{id}:{fee.PaymentResumeRevision ?? 0}:{deadline.Ticks}:{(overdue ? "overdue" : "due")}", fee.Shop.Owner.Email,
            overdue ? "SaveBite: platform fee payment is overdue" : "SaveBite: platform fee payment is due soon",
            $"Platform fee statement {fee.BillingMonth:D2}/{fee.BillingYear} for {fee.ShopNameSnapshot ?? fee.Shop.Name}: {fee.TotalFeeAmount:N0} VND. Deadline: {BusinessEmailQueue.DeadlineText(deadline)}. {(overdue ? "The payment deadline has passed. Resolve this bill to avoid or lift restrictions on your shop." : "Please complete payment before the deadline.")}\n{emails.Link($"/store-owner/platform-fees/{id}")}",
            overdue ? "fee-overdue" : "fee-due", id, deadline, overdue ? null : deadline, ct, fee.TotalFeeAmount);
        await db.SaveChangesAsync(ct);
    }, ct);
}
