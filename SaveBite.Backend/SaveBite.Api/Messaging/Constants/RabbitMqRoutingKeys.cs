namespace SaveBite.Backend.Messaging.Constants;

public static class RabbitMqRoutingKeys
{
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
    
    public const string ModerationAudit = "moderation.audit";
}