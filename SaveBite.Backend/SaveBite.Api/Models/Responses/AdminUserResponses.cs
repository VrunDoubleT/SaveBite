namespace SaveBite.Backend.Models.Responses;

public sealed record UserSummaryResponse(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string Status,
    string CustomerStatus,
    DateTime CreatedAt
);

public sealed record UserDetailsResponse(
    Guid Id,
    string Email,
    string? Phone,
    string FullName,
    string? AvatarUrl,
    string Role,
    string Status,
    string CustomerStatus,
    string ShopStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt
);