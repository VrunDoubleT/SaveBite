using SaveBite.Backend.Configurations;
using System.Linq.Expressions;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
namespace SaveBite.Backend.Helpers;

// Pure refund policy checks shared by commands and boundary tests.
public static class RefundRules
{
    public const int ShopProcessingHours = TimingConfiguration.Refund.ShopProcessingHours;
    public static RefundStatus ExpiredStatus(Refund refund) =>
        refund.Status != RefundStatus.AwaitingCustomerDetails && refund.RefundType == RefundType.ShopCancellation
        && refund.RequestedAmount > refund.TransferredAmount ? RefundStatus.Overdue : RefundStatus.Rejected;
    public static Expression<Func<Refund, bool>> ShopActionRequiredAt(DateTime now) => refund =>
        refund.DestinationConfirmedAt != null && (refund.ReviewDeadlineAt > now || (refund.RefundType == RefundType.ShopCancellation && refund.Status == RefundStatus.Overdue))
        && (refund.RefundType == RefundType.ShopCancellation || refund.Order.Status == OrderStatus.Completed)
        && ((refund.Status == RefundStatus.PendingVerification && refund.RefundType == RefundType.CustomerRequest)
            || ((refund.Status == RefundStatus.AwaitingTransfer || refund.Status == RefundStatus.Overdue) && refund.RequestedAmount > refund.TransferredAmount));

    public const string CustomerConfirmationExpired = "Customer confirmation deadline expired.";
    public static readonly string ShopProcessingExpired = $"Automatically declined by the system because the shop did not resolve this customer refund within {TimingConfiguration.Refund.ShopProcessingHours} hours.";
    public static readonly string MandatoryRefundOverdue = $"Mandatory refund unpaid after {TimingConfiguration.Refund.ShopProcessingHours} hours. The shop is suspended until all overdue mandatory refunds are fully paid.";
    // PostgreSQL timestamps keep microseconds; compare job arguments at the same precision.
    public static bool SameDeadline(DateTime? recorded, DateTime expected) => recorded.HasValue && recorded.Value.Ticks / 10 == expected.Ticks / 10;
    public static DateTime? Deadline(Refund refund) => refund.Status == RefundStatus.AwaitingCustomerDetails
        ? refund.CustomerDetailsDeadlineAt : refund.ReviewDeadlineAt;
    public static void ValidateCustomerDetailsDeadline(Refund refund, DateTime now)
    {
        if ((refund.CustomerDetailsDeadlineAt ?? refund.CreatedAt.AddHours(TimingConfiguration.Refund.CustomerDetailsHours)) <= now)
            throw AppException.Conflict($"The {TimingConfiguration.Refund.CustomerDetailsHours}-hour confirmation deadline has passed. This refund is no longer available.");
    }
    public static bool Active(RefundStatus status) => status is RefundStatus.AwaitingCustomerDetails or RefundStatus.PendingVerification or RefundStatus.AwaitingTransfer or RefundStatus.Overdue;
    public static decimal EligibleAmount(Order order) => Math.Max(0, order.Payments.Sum(CheckoutOrderMapper.Collected)
        - order.Refunds.Sum(x => x.TransferredAmount));
    public static void ValidateRequest(Order order, DateTime now)
    {
        if (order.Status != OrderStatus.Completed || !order.CompletedAt.HasValue)
            throw AppException.Conflict("Only a Completed order is eligible for a customer refund request.");
        if (order.CompletedAt > now || now > order.CompletedAt.Value.AddHours(TimingConfiguration.Refund.CustomerRequestWindowHours))
            throw AppException.Conflict($"Refund requests must be submitted within {TimingConfiguration.Refund.CustomerRequestWindowHours} hours of order completion.");
    }
    public static void ValidateBank(RefundBankRequest request)
    {
        if (request.BankBin == null || request.BankBin.Length != 6 || !request.BankBin.All(char.IsAsciiDigit)
            || string.IsNullOrWhiteSpace(request.BankName) || request.BankName.Length > 100
            || string.IsNullOrEmpty(request.AccountNumber) || request.AccountNumber.Length > 50 || !request.AccountNumber.All(char.IsAsciiDigit)
            || string.IsNullOrWhiteSpace(request.AccountHolder) || request.AccountHolder.Length > 150)
            throw AppException.BadRequest("Supply a valid bank BIN, bank name, account number and account holder.");
    }
    public static void RequireDestinationFields(Refund refund, bool requireBankBin = false)
    {
        ValidateBank(new RefundBankRequest
        {
            BankBin = refund.ReceiverBankBin ?? (requireBankBin ? "" : "000000"),
            BankName = refund.ReceiverBankName!,
            AccountNumber = refund.ReceiverAccountNumber!,
            AccountHolder = refund.ReceiverAccountHolder!
        });
    }
    public static void ValidateDeadline(Refund refund, DateTime now)
    {
        if (!refund.ReviewDeadlineAt.HasValue || refund.ReviewDeadlineAt <= now)
            throw AppException.Conflict($"The {TimingConfiguration.Refund.ShopProcessingHours}-hour shop processing deadline has passed. This refund can no longer be processed.");
    }
    public static void RequireTransferable(Refund refund, DateTime now)
    {
        var overdueMandatory = refund.RefundType == RefundType.ShopCancellation && refund.Status == RefundStatus.Overdue;
        if ((!overdueMandatory && refund.Status != RefundStatus.AwaitingTransfer) || refund.DestinationConfirmedAt == null)
            throw AppException.Conflict("Transfer requires an approved refund with an email-confirmed bank destination.");
        if (refund.RefundType == RefundType.CustomerRequest && refund.Order.Status != OrderStatus.Completed)
            throw AppException.Conflict("Customer refunds require a Completed order.");
        if (refund.RefundType != RefundType.ShopCancellation) ValidateDeadline(refund, now);
        RequireDestinationFields(refund);
        if (refund.RequestedAmount - refund.TransferredAmount <= 0) throw AppException.Conflict("No refund balance remains.");
    }
    // Normalize small client clock skew while enforcing the actual server-side processing deadline.
    public static DateTime ValidateTransfer(Refund refund, DateTime transferredAt, DateTime now)
    {
        RequireTransferable(refund, now);
        if (transferredAt.Kind != DateTimeKind.Utc || transferredAt - now > TimeSpan.FromSeconds(TimingConfiguration.Refund.TransferClockSkewSeconds))
            throw AppException.BadRequest($"Supply a UTC transfer time no more than {TimingConfiguration.Refund.TransferClockSkewSeconds} seconds ahead of the server.");
        var normalized = transferredAt > now ? now : transferredAt;
        if (normalized < (refund.DestinationConfirmedAt ?? refund.CreatedAt)
            || (refund.ReviewedAt.HasValue && normalized < refund.ReviewedAt))
            throw AppException.BadRequest("Supply the actual transfer time after confirmation and approval.");
        return normalized;
    }
}