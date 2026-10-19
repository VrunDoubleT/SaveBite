namespace SaveBite.Backend.Messaging.Events;

// Hangfire publishes this payload inside RabbitMessage<T> after closing, hiding, or expiring a deal.
public sealed class FlashDealEvent
{
    public Guid FlashDealId { get; set; }
}
