namespace SaveBite.Backend.Constants;

// Define shared event names used by backend publishers and frontend subscribers.
public static class RealtimeEvents
{
    public const string ShopPendingOrdersChanged = "shop.orders.pending.changed";
    public const string CustomerOrdersChanged = "customer.orders.changed";
    public const string ShopOrdersChanged = "shop.orders.changed";
    public const string ShopStatusChanged = "shop.status.changed";
    public const string PaymentChanged = "payment.changed";
    public const string PlatformFeeChanged = "platform-fee.changed";
    public const string StaffInvitationsChanged = "staff.invitations.changed";
    public const string ShopStaffChanged = "shop.staff.changed";
    public const string CustomerStatusChanged = "customer.status.changed";
    public const string UserRoleChanged = "user.role.changed";
    public const string ShopApplicationReviewed = "shop.application.reviewed";
    public const string CartChanged = "cart.changed";

    // Keep cart notification reasons beside the shared event names.
    public static class CartReasons
    {
        public const string Updated = "updated";
        public const string Checkout = "checkout";
        public const string DealUnavailable = "deal-unavailable";
    }

    // Keep order notification reasons stable across publishers and recipient filtering.
    public static class OrderReasons
    {
        public const string RestockChanged = "restock-changed";
        public const string RefundChanged = "refund-changed";
        public const string Pending = "pending";
        public const string Confirm = "confirm";
        public const string Reject = "reject";
        public const string Cancelled = "cancelled";
        public const string ConfirmationExpired = "confirmation-expired";
        public const string Preparing = "Preparing";
        public const string ReadyForPickup = "ReadyForPickup";
        public const string Complete = "complete";
        public const string PaymentSucceeded = "payment-succeeded";
        public const string NoShow = "no-show";
    }
    
    //notification
    public const string NotificationReceived = "notification.received";
    public const string NotificationRead = "notification.read";
    public const string NotificationDeleted = "notification.deleted";

    // Moderation & store status
    public const string FeedbackChanged = "feedback.changed";

    // Trust Score & Level Realtime
    public const string TrustLevelChanged = "trust.level.changed";
}
