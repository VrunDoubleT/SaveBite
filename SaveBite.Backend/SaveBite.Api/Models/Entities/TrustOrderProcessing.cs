using SaveBite.Backend.Audit;

namespace SaveBite.Backend.Models.Entities;

// Record successful terminal-order processing even when no active score rule applies.
[NoAudit]
public sealed class TrustOrderProcessing
{
    public Guid OrderId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public decimal CreditedOrderValue { get; set; }
}
