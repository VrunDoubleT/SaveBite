namespace SaveBite.Backend.Models.Responses;

public sealed record UserAddressResponse(
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
    UserAddressResponse? DefaultAddress = null);
