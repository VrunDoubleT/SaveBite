namespace SaveBite.Backend.Models.Responses;

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string? Phone,
    string FullName,
    string? AvatarUrl,
    string Role);
