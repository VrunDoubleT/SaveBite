using System.Text.Json.Serialization;
using SaveBite.Backend.Models.Enums;

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
    DateTime ExpiresAt
);

public sealed record ShopStaffResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    string? StaffNickname,
    string DisplayName ,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ShopStaffStatus Status,
    DateTime JoinedAt
);

public sealed record AssociatedShopResponse(
    Guid Id,
    Guid ShopId,
    string ShopName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ShopStaffStatus Status,
    DateTime JoinedAt
);

public sealed record StaffActivityLogResponse(
    Guid Id,
    Guid ActionBy,
    string Action,
    string Description,
    DateTime CreatedAt
);

public sealed record StaffShopInfoResponse(
    StaffShopDetailResponse Shop,
    ShopStaffResponse Staff
);

public sealed record StaffShopDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string AddressLine,
    string? Ward,
    string? District,
    string? City,
    string? LogoUrl,
    string? CoverImageUrl,
    TimeOnly? OpeningTime,
    TimeOnly? ClosingTime,

    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ShopStatus Status
);