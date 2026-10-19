namespace SaveBite.Backend.Models.DTOs;

// Capture the operator identity, shop role and name recorded in order history.
public sealed record ShopOrderActor(Guid Id, string Role, string Name);
