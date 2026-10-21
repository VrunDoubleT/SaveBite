using SaveBite.Backend.Models.Common;

namespace SaveBite.Backend.Models.Responses;

// Describe a cart variant with current availability, quantity and server-calculated amounts.
public sealed record CartItemResponse
{
    public Guid Id { get; init; }
    public Guid FlashDealId { get; init; }
    public Guid FlashDealVariantId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string VariantName { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal OriginalPrice { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public int AvailableQuantity { get; init; }
    public decimal Subtotal { get; init; }
    public DateTime OrderEndTime { get; init; }
    public bool CanUpdateQuantity { get; init; }
    public bool IsAvailable { get; init; }
    public string? UnavailableReason { get; init; }

}

// Group cart items and totals for one shop eligible for a separate checkout.
public sealed record CartShopResponse
{
    public Guid ShopId { get; init; }
    public string ShopName { get; init; } = string.Empty;
    public string? ShopLogoUrl { get; init; }
    public string ShopAddress { get; init; } = string.Empty;
    public IReadOnlyList<CartItemResponse> Items { get; init; } = [];
    public int TotalQuantity { get; init; }
    public decimal Subtotal { get; init; }
    public bool CanCheckout { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }

}

// Return the grouped cart and overall quantity and deal counts.
public sealed record CartResponse
{
    public IReadOnlyList<CartShopResponse> Shops { get; init; } = [];
    public int TotalQuantity { get; init; }
}

// Return lightweight whole-cart counts for the navigation badge.
public sealed record CartSummaryResponse
{
    public int TotalQuantity { get; init; }
    public int TotalFlashDeals { get; init; }

}
// Combine a cursor page of complete shop groups with whole-cart counts.
public sealed record CartPageResponse
{
    public CursorPagedResult<CartShopResponse> Page { get; init; } = null!;
    public int TotalQuantity { get; init; }
    public int TotalFlashDeals { get; init; }

}

// Return the changed shop snapshot and summary needed to patch the cart UI.
public sealed record CartMutationResponse
{
    public Guid ShopId { get; init; }
    public CartShopResponse? Shop { get; init; }
    public CartSummaryResponse Summary { get; init; } = null!;
}
