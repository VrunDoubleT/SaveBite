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
    // Retain only for reading historical audit rows; new deadline expirations become Rejected.
    Overdue
}
