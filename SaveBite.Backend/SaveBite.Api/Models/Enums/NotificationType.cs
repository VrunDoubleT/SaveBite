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
    
    ProductNeedsRevision = 20,

    OrderNoShow = 21,
    StaffInvitationDeclined = 22,
    StaffInvitationExpired = 23,
    StaffJoinedShop = 24,
    FeedbackReceived = 25,
    FeedbackReplied = 26,
    PlatformFeeStatementIssued = 27,
    ProductContentApproved = 28,
    ShopStatusChanged = 29,
    AccountStatusChanged = 30,
    FeedbackRemovedByAdmin = 31,
    PlatformFeeDueSoon = 32,
    PlatformFeeOverdue = 33,
    PlatformFeePaid = 34,
    OrderCancelledByCustomer = 35,
    OrderPaymentExpired = 36,
    StaffStatusChanged = 37,
    StaffLeftShop = 38,
    ProductRemoved = 39,
    TrustLevelChanged = 40,
    PickupDeadlineApproaching = 41,
    ShopApplicationSubmitted = 42
}
