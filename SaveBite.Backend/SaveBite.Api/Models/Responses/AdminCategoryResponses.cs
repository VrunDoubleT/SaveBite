namespace SaveBite.Backend.Models.Responses;

public sealed record AdminCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);