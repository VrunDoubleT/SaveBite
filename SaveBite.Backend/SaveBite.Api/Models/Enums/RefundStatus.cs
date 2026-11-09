namespace SaveBite.Backend.Models.Enums;

public enum RefundStatus
{
    AwaitingCustomerDetails,
    PendingVerification,
    AwaitingTransfer,
    Rejected,
    Transferred,
    Completed,
    Withdrawn,
    // Mandatory refunds remain payable after their processing deadline.
    Overdue
}
