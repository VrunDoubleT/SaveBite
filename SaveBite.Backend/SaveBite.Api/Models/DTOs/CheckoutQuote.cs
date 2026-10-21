using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Models.DTOs;

// Cache the customer's location and agreed calculations for later checkout validation.
public sealed class CheckoutQuote
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ShopId { get; set; }
    public CheckoutLocation? Location { get; set; }
    public PickupEstimate? Pickup { get; set; }
    public decimal RequiredDeposit { get; set; }
    public decimal TotalAmount { get; set; }
    public double ShopLatitude { get; set; }
    public double ShopLongitude { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
