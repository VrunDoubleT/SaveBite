namespace SaveBite.Backend.Models.Responses;

// Describe one customer-owned payment and its associated order and shop.
public sealed class CustomerPaymentResponse
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public string? ShopLogoUrl { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal CollectedAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaidAt { get; init; }
}

// Show a selected payment together with its recorded order details.
public sealed class CustomerPaymentDetailResponse
{
    public CustomerPaymentResponse Payment { get; init; } = null!;
    public CheckoutOrderResponse Order { get; init; } = null!;
}
