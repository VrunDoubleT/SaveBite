namespace SaveBite.Backend.Models.Responses;

public class RefundHistoryResponse
{
    public Guid Id { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? TransferReference { get; set; }
    public string? ProofUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class RefundResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ShopLogoUrl { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAvatarUrl { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerLevelName { get; set; }
    public int? CustomerLevelRank { get; set; }
    public bool RequiresShopAction { get; set; }
    public string ShopAction { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal OrderRefundedAmount { get; set; }
    public decimal OrderNetAmount { get; set; }
    public IReadOnlyList<RefundItemResponse> Items { get; set; } = [];
    public decimal Amount { get; set; }
    public decimal TransferredAmount { get; set; }
    public string? Reason { get; set; }
    public string? RejectReason { get; set; }
    public string? ReceiverMethod { get; set; }
    public string? ReceiverQrImageUrl { get; set; }
    public string? BankBin { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountHolder { get; set; }
    public DateTime? ConfirmationExpiresAt { get; set; }
    public DateTime? DestinationConfirmedAt { get; set; }
    public DateTime? PendingVerificationAt { get; set; }
    public DateTime? CustomerDetailsDeadlineAt { get; set; }
    public DateTime? ReviewDeadlineAt { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsPlatformFeeExempt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? TransferReference { get; set; }
    public DateTime? TransferredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<string> EvidenceUrls { get; set; } = [];
    public IReadOnlyList<string> TransferProofUrls { get; set; } = [];
    public IReadOnlyList<RefundHistoryResponse> History { get; set; } = [];
}
public class RefundQrResponse
{
    public Guid RefundId { get; set; }
    public decimal Amount { get; set; }
    public string BankBin { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class RefundImageResponse
{
    public string Url { get; set; } = string.Empty;
    public string? BankBin { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountHolder { get; set; }
}

public class RefundItemResponse
{
    public string Reason { get; set; } = string.Empty;
    public IReadOnlyList<string> EvidenceUrls { get; set; } = [];
    public Guid OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }
    public int PurchasedQuantity { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
