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
    DefaultAddressResponse? DefaultAddress = null,
    CurrentUserShopResponse? Shop = null);

public sealed record CurrentUserShopResponse(
    Guid ShopId,
    string Status,
    bool SuspendedForPlatformFees,
    string? SuspensionReason,
    bool SuspendedByAdmin = false,
    bool SuspendedForOverdueRefunds = false);
