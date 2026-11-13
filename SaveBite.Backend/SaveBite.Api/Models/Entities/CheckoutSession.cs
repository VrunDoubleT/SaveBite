using SaveBite.Backend.Audit;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

// An inventory reservation and payment attempt, not a submitted order.
[NoAudit]
public sealed class CheckoutSession
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ShopId { get; set; }
    public Guid QuoteId { get; set; }
    public Guid RequestId { get; set; }
    public OrderStatus Status { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime PaymentExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
