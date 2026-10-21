namespace SaveBite.Backend.Models.Entities;

// Persist an order event and its delivery progress alongside the order transaction.
public sealed class OrderEventOutbox
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string RoutingKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
