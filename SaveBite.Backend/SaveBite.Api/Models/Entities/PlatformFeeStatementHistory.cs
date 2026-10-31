using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class PlatformFeeStatementHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StatementId { get; set; }

    public StatementChangeType ChangeType { get; set; }
    public decimal PreviousAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal NewAmount { get; set; }

    public string Reason { get; set; } = string.Empty;
    public Guid? RelatedRefundId { get; set; }
    public Guid? RelatedOrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PlatformFeeStatement Statement { get; set; } = null!;
}