namespace SaveBite.Backend.Models.Enums;

public enum NotificationType
{
    OrderCreated = 1,
    OrderAccepted = 2,
    OrderRejected = 3,
    PickupReminder = 4,
    OrderCompleted = 5,

    PaymentSucceeded = 6,
    PaymentFailed = 7,

    RefundRequestCreated = 8,
    RefundApproved = 9,
    RefundRejected = 10,
    RefundTransferred = 11,

    StaffInvitationReceived = 12,
    StaffInvitationAccepted = 13,

    ShopApplicationApproved = 14,
    ShopApplicationRejected = 15,
    ShopApplicationNeedsRevision = 16,

    FlashDealAvailable = 17,
    TrustScoreChanged = 18,
    NoShowPenaltyCreated = 19,
    
    ProductNeedsRevision = 20
}