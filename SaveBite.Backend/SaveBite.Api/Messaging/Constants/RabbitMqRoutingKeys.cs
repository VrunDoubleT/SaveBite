namespace SaveBite.Backend.Messaging.Constants;

public static class RabbitMqRoutingKeys
{
    public const string ShopDealOpened = "shop.deal.opened";
    public const string RefundTransferred = "refund.transferred";
    public const string InventoryChanged = "inventory.changed";
    public const string NotificationRequested = "notification.requested";
    public const string UserVerified = "user.verified";
    public const string OrderCompleted = "order.completed";
    public const string OrderPending = "order.pending";
    public const string OrderNoShow = "order.noshow";
  
    public const string EmailRoutingKey = "email";

    // Flash deal routing keys
    public const string FlashDealOpened = "flashdeal.opened";
    public const string FlashDealClosed = "flashdeal.closed";
    public const string FlashDealHidden = "flashdeal.hidden";
    public const string FlashDealExpired = "flashdeal.expired";
    
    // Deal Publication & Location Geo routing keys (Section 9)
    public const string DealPublished = "deal.published";
    public const string DealReopened = "deal.reopened";
    public const string UserDefaultAddressChanged = "internal.user.default-address.changed";
}
