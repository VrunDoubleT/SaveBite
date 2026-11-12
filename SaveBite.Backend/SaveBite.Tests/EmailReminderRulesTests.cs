using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using Xunit;
namespace SaveBite.Tests;

public class EmailReminderRulesTests
{
    private static readonly DateTime Start = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);
    private static Refund Customer() => new() { CreatedAt = Start, CustomerDetailsDeadlineAt = Start.AddHours(24),
        Status = RefundStatus.AwaitingCustomerDetails, RequestedAmount = 100000 };
    private static Refund Shop() => new() { CreatedAt = Start, ReviewDeadlineAt = Start.AddHours(72),
        DestinationConfirmedAt = Start, Status = RefundStatus.AwaitingTransfer, RequestedAmount = 100000,
        RefundType = RefundType.ShopCancellation };

    [Theory]
    [InlineData(11, null)] [InlineData(12, "after-12h")] [InlineData(17, "after-12h")]
    [InlineData(18, "final-6h")] [InlineData(23, "final-6h")] [InlineData(24, null)] [InlineData(30, null)]
    public void CustomerReminderUsesValidWindows(int hour, string? expected) =>
        Assert.Equal(expected, EmailReminderRules.CustomerDetails(Customer(), Start.AddHours(hour))?.Stage);

    [Fact] public void CustomerReminderStopsAfterConfirmationOrClosing()
    {
        var refund = Customer(); refund.DestinationConfirmedAt = Start.AddHours(1);
        Assert.Null(EmailReminderRules.CustomerDetails(refund, Start.AddHours(20)));
        refund.DestinationConfirmedAt = null; refund.Status = RefundStatus.Withdrawn;
        Assert.Null(EmailReminderRules.CustomerDetails(refund, Start.AddHours(20)));
    }
    [Fact] public void UnconfirmedLegacyVerificationStillReceivesCustomerReminder()
    {
        var refund = Customer(); refund.Status = RefundStatus.PendingVerification;
        Assert.Equal("final-6h", EmailReminderRules.CustomerDetails(refund, Start.AddHours(20))!.Stage);
    }
    [Fact] public void DelayedScanChoosesOnlyFinalCustomerReminder()
    {
        Assert.Equal("final-6h", EmailReminderRules.CustomerDetails(Customer(), Start.AddHours(22))!.Stage);
    }
    [Fact] public void EarlierCustomerReminderExpiresWhenFinalWindowStarts()
    {
        Assert.Equal(Start.AddHours(18), EmailReminderRules.CustomerDetails(Customer(), Start.AddHours(12))!.ExpiresAt);
    }
    [Theory]
    [InlineData(47, null)] [InlineData(48, "due-24h")] [InlineData(69, "due-24h")]
    [InlineData(70, "final-2h")] [InlineData(71, "final-2h")] [InlineData(72, "overdue")] [InlineData(100, "overdue")]
    public void ShopTransferReminderUsesActualProcessingDeadline(int hour, string? expected) =>
        Assert.Equal(expected, EmailReminderRules.ShopTransfer(Shop(), Start.AddHours(hour))?.Stage);
    [Fact] public void CompletedRefundNeverRemindsShop()
    {
        var refund = Shop(); refund.TransferredAmount = refund.RequestedAmount;
        Assert.Null(EmailReminderRules.ShopTransfer(refund, Start.AddHours(70)));
    }
    [Fact] public void UnconfirmedDestinationNeverRemindsShopToTransfer()
    {
        var refund = Shop(); refund.DestinationConfirmedAt = null;
        Assert.Null(EmailReminderRules.ShopTransfer(refund, Start.AddHours(70)));
    }
    [Fact] public void CustomerRequestDoesNotRemainPayableAfterDeadline()
    {
        var refund = Shop(); refund.RefundType = RefundType.CustomerRequest;
        Assert.Null(EmailReminderRules.ShopTransfer(refund, Start.AddHours(72)));
    }
    [Fact] public void PartialMandatoryRefundStillRemindsRemainingBalance()
    {
        var refund = Shop(); refund.TransferredAmount = 20000; refund.Status = RefundStatus.Overdue;
        Assert.Equal("overdue", EmailReminderRules.ShopTransfer(refund, Start.AddHours(80))!.Stage);
    }
    [Theory]
    [InlineData(47, false)] [InlineData(48, true)] [InlineData(71, true)] [InlineData(72, false)]
    public void InvitationRemindsOnlyInFinalDay(int hour, bool expected)
    {
        var invitation = new StaffInvitation { InvitedAt = Start, Status = "Pending" };
        Assert.Equal(expected, EmailReminderRules.PendingInvitation(invitation, Start.AddHours(hour)));
    }
    [Fact] public void RespondedInvitationNeverReminds()
    {
        var invitation = new StaffInvitation { InvitedAt = Start, Status = "Accepted", RespondedAt = Start };
        Assert.False(EmailReminderRules.PendingInvitation(invitation, Start.AddHours(50)));
    }
    [Fact] public void PaidPausedOrDraftFeeDoesNotRemind()
    {
        var fee = new PlatformFeeStatement { Status = PlatformFeeStatementStatus.Issued, DueAt = Start, TotalFeeAmount = 10000 };
        Assert.True(EmailReminderRules.UnpaidFee(fee));
        fee.PaymentPauseReason = "Unavailable"; Assert.False(EmailReminderRules.UnpaidFee(fee));
        fee.PaymentPauseReason = null; fee.PaidAt = Start; Assert.False(EmailReminderRules.UnpaidFee(fee));
        fee.PaidAt = null; fee.Status = PlatformFeeStatementStatus.Draft; Assert.False(EmailReminderRules.UnpaidFee(fee));
    }
}
