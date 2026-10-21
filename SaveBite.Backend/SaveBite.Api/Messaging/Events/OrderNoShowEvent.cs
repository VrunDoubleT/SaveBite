namespace SaveBite.Backend.Messaging.Events;

// Describe a committed no-show transition for downstream consumers.
public sealed record OrderNoShowEvent(Guid OrderId, Guid ShopId, Guid CustomerId, DateTime HardDeadlineAt, DateTime NoShowAt);
