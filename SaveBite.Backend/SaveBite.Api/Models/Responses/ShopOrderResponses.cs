using SaveBite.Backend.Models.Common;

namespace SaveBite.Backend.Models.Responses;

// Show a recorded order payment, collection method and any active QR link.
public sealed record ShopOrderPaymentResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime? PaidAt { get; init; }
    public string? CheckoutUrl { get; init; }
    public string? QrCode { get; init; }
    public decimal CollectedAmount { get; init; }

}
// Show a status transition together with the recorded actor identity.
public sealed record ShopOrderHistoryResponse
{
    public string? FromStatus { get; init; }
    public string ToStatus { get; init; } = string.Empty;
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid? ActorId { get; init; }
    public string? ActorRole { get; init; }
    public string? ActorName { get; init; }

}
// Show the refund request, destination verification and recorded transfer progress.
public sealed record OrderRefundResponse
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Reason { get; init; }
    public string? BankName { get; init; }
    public string? AccountNumber { get; init; }
    public string? AccountHolder { get; init; }
    public DateTime? ConfirmationExpiresAt { get; init; }
    public DateTime? DestinationConfirmedAt { get; init; }
    public string? TransferReference { get; init; }
    public decimal TransferredAmount { get; init; }

}
// Combine shop order data, customer identity, payments, history and refund information.
public sealed record ShopOrderResponse
{
    public CheckoutOrderResponse Order { get; init; } = null!;
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public DateTime? ConfirmationDeadlineAt { get; init; }
    public bool IsOwner { get; init; }
    public IReadOnlyList<ShopOrderPaymentResponse> Payments { get; init; } = [];
    public IReadOnlyList<ShopOrderHistoryResponse> History { get; init; } = [];
    public OrderRefundResponse? Refund { get; init; }

}
// Combine a numbered shop-order page with counts for its status tabs.
public sealed record ShopOrderPageResponse
{
    public PagedResult<ShopOrderResponse> Page { get; init; } = null!;
    public IReadOnlyDictionary<string, int> StatusCounts { get; init; } = new Dictionary<string, int>();
}
