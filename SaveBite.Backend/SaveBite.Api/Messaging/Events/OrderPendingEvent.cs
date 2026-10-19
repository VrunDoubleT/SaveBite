namespace SaveBite.Backend.Messaging.Events;

// Describe a submitted order and its recorded confirmation and pickup deadlines.
public sealed record OrderPendingEvent(Guid OrderId, Guid ShopId, DateTime PendingAt, DateTime ConfirmationDeadlineAt);
