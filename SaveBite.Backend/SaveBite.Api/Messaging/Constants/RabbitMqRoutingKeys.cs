namespace SaveBite.Backend.Messaging.Constants;

public static class RabbitMqRoutingKeys
{
    public const string EmailRoutingKey = "email";

    // Flash deal routing keys
    public const string FlashDealOpened = "flashdeal.opened";
    public const string FlashDealClosed = "flashdeal.closed";
    public const string FlashDealHidden = "flashdeal.hidden";
    public const string FlashDealExpired = "flashdeal.expired";
}