using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public Guid ShopId { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.OnlinePayment;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public decimal? DistanceKm { get; set; }
    public int? EtaMinutes { get; set; }

    public DateTime? SoftDeadlineAt { get; set; }
    public DateTime? HardDeadlineAt { get; set; }

    public bool IsLateArrival { get; set; }
    public bool NoShow { get; set; }

    public string? CancelReason { get; set; }
    public Guid? CancelledBy { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ExpiredAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Customer { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
    public User? CancelledByUser { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    public ICollection<StaffActivityLog> StaffActivityLogs { get; set; } = new List<StaffActivityLog>();
    public ICollection<TrustScoreHistory> TrustScoreHistories { get; set; } = new List<TrustScoreHistory>();
    public ICollection<UserTrustLevelHistory> TrustLevelHistories { get; set; } = new List<UserTrustLevelHistory>();

    public NoShowPenalty? NoShowPenalty { get; set; }
    public NoShowPenaltyOrderHistory? RecoveryPenaltyHistory { get; set; }
    public OrderPlatformFee? PlatformFee { get; set; }
}
