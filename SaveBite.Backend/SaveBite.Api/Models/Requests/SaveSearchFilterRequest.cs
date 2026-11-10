namespace SaveBite.Backend.Models.Requests;

public sealed record SaveSearchFilterRequest(
    string? Keyword = null,
    string? Category = null,
    string? Distance = null,
    string? Price = null,
    string? ShopName = null,
    double RadiusInKm = 5.0,
    string? SessionKey = null);
