namespace SaveBite.Backend.Constants;

// Define shared event names used by backend publishers and frontend subscribers.
public static class RealtimeEvents
{
    public const string ShopPendingOrdersChanged = "shop.orders.pending.changed";
    public const string CustomerOrdersChanged = "customer.orders.changed";
    public const string ShopOrdersChanged = "shop.orders.changed";
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
}
