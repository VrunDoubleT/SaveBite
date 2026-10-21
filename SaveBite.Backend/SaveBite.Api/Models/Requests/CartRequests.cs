namespace SaveBite.Backend.Models.Requests;

// Specify a deal variant and the quantity to add without client-supplied prices.
public sealed record AddCartItemRequest(Guid FlashDealVariantId, int Quantity);
// Specify the replacement quantity for an existing cart item.
public sealed record UpdateCartItemQuantityRequest(int Quantity);
