namespace SaveBite.Backend.Models.Responses;

public sealed record StaffInvitationResponse(
    Guid Id,
    Guid ShopId,
    string ShopName,
    Guid InvitedUserId,
    string InvitedUserName,
    string InvitedUserEmail,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? ShopLogoUrl = null,
    string? ShopCoverImageUrl = null,
    string? ShopAddress = null
);
