namespace SaveBite.Backend.Models.Entities;

// Keep immutable snapshots for every refund decision, destination change and transfer.
public class RefundStatusHistory
{
    public Guid Id { get; set; }
    public Guid RefundId { get; set; }
    public RefundStatus? FromStatus { get; set; }
    public RefundStatus ToStatus { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? TransferReference { get; set; }
    public string? ProofUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public Refund Refund { get; set; } = null!;
}
