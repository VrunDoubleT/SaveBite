namespace SaveBite.Backend.Messaging.Events;

// Carry order identity; consumers read authoritative purchase data from PostgreSQL.
public sealed record OrderCompletedEvent(Guid OrderId, Guid ShopId, Guid CustomerId, DateTime CompletedAt);
