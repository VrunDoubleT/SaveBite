namespace SaveBite.Backend.Models.Enums;

public enum TrustScoreEventType
{
    OrderCompleted = 1,
    NoShow = 2,
    // Accumulated order value was reclaimed because a completed order was refunded (score unchanged).
    RefundReclaimed = 3
}