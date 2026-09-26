using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    public PaymentType PaymentType { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }

    public string? GatewayTransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
}
