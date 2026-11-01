using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class Refund
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    public RefundType RefundType { get; set; }
    public RefundStatus Status { get; set; }

    public ICollection<RefundItem> Items { get; set; } = new List<RefundItem>();
    public Guid InitiatedBy { get; set; }

    public string? Reason { get; set; }

    public decimal RequestedAmount { get; set; }
    public decimal TransferredAmount { get; set; }
    
    public RefundReceiverMethod? ReceiverMethod { get; set; }

    public string? ReceiverBankName { get; set; }
    public string? ReceiverAccountNumber { get; set; }
    public string? ReceiverAccountHolder { get; set; }
    public string? ReceiverQrImageUrl { get; set; }

    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }

    public string? TransferReference { get; set; }
    public DateTime? TransferredAt { get; set; }

    public string? VerificationCodeHash { get; set; }
    public DateTime? VerificationExpiresAt { get; set; }
    public DateTime? DestinationConfirmedAt { get; set; }
    public int VerificationAttempts { get; set; }

    public string? ReceiverBankBin { get; set; }
    public DateTime? VerificationIssuedAt { get; set; }
    public DateTime? PendingVerificationAt { get; set; }
    public DateTime? CustomerDetailsDeadlineAt { get; set; }
    public string? DeadlineJobId { get; set; }
    public DateTime? ReviewDeadlineAt { get; set; }
    public DateTime? AccumulatedValueReclaimedAt { get; set; }
    public ICollection<RefundStatusHistory> History { get; set; } = new List<RefundStatusHistory>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public User InitiatedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public ICollection<RefundEvidenceImage> EvidenceImages { get; set; } = new List<RefundEvidenceImage>();
    public PlatformFeeAdjustment? PlatformFeeAdjustment { get; set; }
}
