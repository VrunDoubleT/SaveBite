namespace SaveBite.Backend.Models.Entities;

public enum RefundStatus
{
    AwaitingCustomerDetails,
    PendingVerification,
    AwaitingTransfer,
    Rejected,
    Transferred,
    Completed,
    Withdrawn
}