using SaveBite.Backend.Configurations;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
namespace SaveBite.Backend.Helpers;

public sealed record ReminderWindow(string Stage, DateTime? ExpiresAt);

public static class EmailReminderRules
{
    public static ReminderWindow? CustomerDetails(Refund refund, DateTime now)
    {
        if (refund.DestinationConfirmedAt != null || refund.Status is not (RefundStatus.AwaitingCustomerDetails or RefundStatus.PendingVerification)
            || refund.RequestedAmount <= refund.TransferredAmount || refund.CustomerDetailsDeadlineAt is not DateTime end || now >= end) return null;
        if (now >= end.AddHours(-TimingConfiguration.EmailReminders.CustomerDetailsFinalLeadHours)) return new("final-6h", end);
        return now >= refund.CreatedAt.AddHours(TimingConfiguration.EmailReminders.CustomerDetailsFirstHours) ? new("after-12h", end.AddHours(-TimingConfiguration.EmailReminders.CustomerDetailsFinalLeadHours)) : null;
    }

    public static ReminderWindow? ShopTransfer(Refund refund, DateTime now)
    {
        if (refund.DestinationConfirmedAt == null || refund.RequestedAmount <= refund.TransferredAmount
            || refund.Status is not (RefundStatus.AwaitingTransfer or RefundStatus.Overdue)
            || refund.ReviewDeadlineAt is not DateTime end) return null;
        if (now >= end) return refund.RefundType == RefundType.ShopCancellation ? new("overdue", null) : null;
        if (now >= end.AddHours(-TimingConfiguration.EmailReminders.ShopTransferFinalLeadHours)) return new("final-2h", end);
        return now >= end.AddHours(-TimingConfiguration.EmailReminders.ShopTransferFirstLeadHours) ? new("due-24h", end.AddHours(-TimingConfiguration.EmailReminders.ShopTransferFinalLeadHours)) : null;
    }

    public static DateTime InvitationDeadline(StaffInvitation invitation) => invitation.InvitedAt.AddDays(TimingConfiguration.StaffInvitations.LifetimeDays);
    public static bool PendingInvitation(StaffInvitation invitation, DateTime now) =>
        invitation.Status == nameof(StaffInvitationStatus.Pending) && invitation.RespondedAt == null
        && now >= InvitationDeadline(invitation).AddHours(-TimingConfiguration.EmailReminders.InvitationLeadHours) && now < InvitationDeadline(invitation);

    public static bool UnpaidFee(PlatformFeeStatement statement) =>
        statement.Status is PlatformFeeStatementStatus.Issued or PlatformFeeStatementStatus.Overdue
        && statement.PaidAt == null && statement.PaymentPauseReason == null && statement.DueAt != null && statement.TotalFeeAmount > 0;
}
