namespace SaveBite.Backend.Messaging.Constants;

public static class RabbitMqRoutingKeys
{
    public const string OrderPending = "order.pending";
    public const string OrderNoShow = "order.noshow";

    public const string EmailRoutingKey = "email";
}