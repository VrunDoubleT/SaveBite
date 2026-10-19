using SaveBite.Backend.Models.Common;

namespace SaveBite.Backend.Models.Responses;

// Describe the order-value interval and fixed deposit applied to checkout.
public sealed record DepositRangeSnapshot
{
    public Guid Id { get; init; }
    public decimal FromOrderAmount { get; init; }
    public decimal ToOrderAmount { get; init; }
    public decimal DepositAmount { get; init; }

}
// Explain the applied trust level, deposit amount and payment restrictions.
public sealed record CheckoutPolicySnapshot
{
    public Guid? LevelId { get; init; }
    public string LevelName { get; init; } = string.Empty;
    public int? LevelRank { get; init; }
    public bool IsDefaultLevel { get; init; }
    public Guid? PolicyId { get; init; }
    public DateTime? PolicyUpdatedAt { get; init; }
    public decimal? FreeOrderAmount { get; init; }
    public decimal? MaxDepositOrderAmount { get; init; }
    public bool PayAtShopAllowed { get; init; }
    public string? RestrictionCode { get; init; }
    public string? RestrictionReason { get; init; }
    public decimal RequiredDeposit { get; init; }
    public DepositRangeSnapshot? AppliedRange { get; init; }
    public IReadOnlyList<DepositRangeSnapshot> Ranges { get; init; } = [];
}
// Expose route estimates and the agreed soft, hard and deal pickup deadlines.
public sealed record PickupEstimate
{
    public decimal DistanceKm { get; init; }
    public int EtaMinutes { get; init; }
    public int BufferMinutes { get; init; }
    public DateTime SoftDeadlineAt { get; init; }
    public DateTime HardDeadlineAt { get; init; }
    public DateTime DealEndsAt { get; init; }
    public bool BufferWasCapped { get; init; }

}
// Return current shop cart data and payment eligibility before GPS estimation.
public sealed record CheckoutContextResponse
{
    public CartShopResponse Shop { get; init; } = null!;
    public CheckoutPolicySnapshot Policy { get; init; } = null!;
    public bool OnlinePaymentAvailable { get; init; }
    public string? PaymentUnavailableReason { get; init; }
    public bool IsSimulatedPayment { get; init; }

}
// Return the temporary quote and calculations the customer reviews before ordering.
public sealed record CheckoutQuoteResponse
{
    public Guid Id { get; init; }
    public CartShopResponse Shop { get; init; } = null!;
    public CheckoutPolicySnapshot Policy { get; init; } = null!;
    public PickupEstimate Pickup { get; init; } = null!;
    public bool OnlinePaymentAvailable { get; init; }
    public string? PaymentUnavailableReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsSimulatedPayment { get; init; }
    public CheckoutRouteResponse? Route { get; init; }

}
// Expose the item quantities and prices recorded when the order was placed.
public sealed record CheckoutOrderItemResponse
{
    public Guid FlashDealVariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
    public string? ImageUrl { get; init; }
    public Guid? FlashDealId { get; init; }

}
// Expose order identity, payment totals, pickup deadlines and current order state.
public sealed record CheckoutOrderResponse
{
    public Guid Id { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public Guid ShopId { get; init; }
    public string ShopName { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public decimal DepositAmount { get; init; }
    public decimal RequiredOnlineAmount { get; init; }
    public decimal RemainingAtShop { get; init; }
    public DateTime? SoftDeadlineAt { get; init; }
    public DateTime? HardDeadlineAt { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public bool PaymentReviewRequired { get; init; }
    public string? CheckoutUrl { get; init; }
    public CheckoutPolicySnapshot? Policy { get; init; }
    public IReadOnlyList<CheckoutOrderItemResponse> Items { get; init; } = [];
    public string? PaymentMessage { get; init; }
    public bool CanCancel { get; init; }
    public string? CancellationUnavailableReason { get; init; }
    public string? CancellationPolicy { get; init; }
    public string? ShopLogoUrl { get; init; }
    public string? ShopAddress { get; init; }
    public DateTime? CreatedAt { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount { get; init; }

}

// Combine a cursor page of customer orders with status-filter counts.
public sealed record OrderHistoryPageResponse
{
    public CursorPagedResult<CheckoutOrderResponse> Page { get; init; } = null!;
    public IReadOnlyDictionary<string, int> StatusCounts { get; init; } = new Dictionary<string, int>();
}

// Expose route geometry for the customer's trip to the selected shop.
public sealed record CheckoutRouteResponse
{
    public double OriginLatitude { get; init; }
    public double OriginLongitude { get; init; }
    public double ShopLatitude { get; init; }
    public double ShopLongitude { get; init; }
    public string EncodedPolyline { get; init; } = string.Empty;
}
