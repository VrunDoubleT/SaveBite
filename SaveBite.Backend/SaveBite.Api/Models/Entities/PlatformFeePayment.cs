using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class PlatformFeePayment
{
    public Guid Id { get; set; }
    public Guid StatementId { get; set; }
    public Guid ShopId { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? GatewayTransactionId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PlatformFeeStatement Statement { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
}
