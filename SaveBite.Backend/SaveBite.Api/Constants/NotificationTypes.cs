namespace SaveBite.Backend.Constants;

public static class NotificationTypes
{
    public const string OrderCreated = "OrderCreated";
    public const string OrderAccepted = "OrderAccepted";
    public const string OrderRejected = "OrderRejected";
    public const string PickupReminder = "PickupReminder";
    public const string OrderCompleted = "OrderCompleted";

    public const string PaymentSucceeded = "PaymentSucceeded";
    public const string PaymentFailed = "PaymentFailed";

    public const string RefundRequestCreated = "RefundRequestCreated";
    public const string RefundApproved = "RefundApproved";
    public const string RefundRejected = "RefundRejected";
    public const string RefundTransferred = "RefundTransferred";

    public const string StaffInvitationReceived = "StaffInvitationReceived";
    public const string StaffInvitationAccepted = "StaffInvitationAccepted";
    public const string StaffInvitationDeclined = "StaffInvitationDeclined";
    public const string StaffInvitationExpired = "StaffInvitationExpired";
    public const string StaffJoinedShop = "StaffJoinedShop";
    public const string StaffStatusChanged = "StaffStatusChanged";
    public const string StaffLeftShop = "StaffLeftShop";

    public const string ShopApplicationSubmitted = "ShopApplicationSubmitted";
    public const string ShopApplicationApproved = "ShopApplicationApproved";
    public const string ShopApplicationRejected = "ShopApplicationRejected";
    public const string ShopApplicationNeedsRevision = "ShopApplicationNeedsRevision";
    public const string ShopStatusChanged = "ShopStatusChanged";

    public const string FlashDealAvailable = "FlashDealAvailable";
    public const string ProductNeedsRevision = "ProductNeedsRevision";
    public const string ProductContentApproved = "ProductContentApproved";
    public const string ProductRemoved = "ProductRemoved";

    public const string FeedbackReceived = "FeedbackReceived";
    public const string FeedbackReplied = "FeedbackReplied";
    public const string FeedbackRemovedByAdmin = "FeedbackRemovedByAdmin";

    public const string TrustScoreChanged = "TrustScoreChanged";
    public const string TrustLevelChanged = "TrustLevelChanged";
    public const string NoShowPenaltyCreated = "NoShowPenaltyCreated";
    public const string AccountStatusChanged = "AccountStatusChanged";

    public const string PlatformFeeStatementIssued = "PlatformFeeStatementIssued";
    public const string PlatformFeeDueSoon = "PlatformFeeDueSoon";
    public const string PlatformFeeOverdue = "PlatformFeeOverdue";
    public const string PlatformFeePaid = "PlatformFeePaid";

    public const string OrderCancelledByCustomer = "OrderCancelledByCustomer";
    public const string OrderPaymentExpired = "OrderPaymentExpired";
    public const string OrderNoShow = "OrderNoShow";
    public const string PickupDeadlineApproaching = "PickupDeadlineApproaching";
}
