namespace SaveBite.Backend.Models.Responses;

public sealed record DefaultAddressResponse(
    Guid Id,
    string? Label,
    string AddressLine,
    string? Ward,
    string? District,
    string? City,
    double Latitude,
    double Longitude,
    bool IsDefault);

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string? Phone,
    string FullName,
    string? AvatarUrl,
    string Role,
    string CustomerStatus,
    DefaultAddressResponse? DefaultAddress = null);
