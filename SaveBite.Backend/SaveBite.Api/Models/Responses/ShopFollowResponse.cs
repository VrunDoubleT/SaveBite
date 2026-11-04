using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Responses;

public sealed record ShopFollowResponse(Guid ShopId, string ShopName, string? LogoUrl, string AddressLine,
    ShopStatus ShopStatus, ShopNotificationStatus NotificationStatus, DateTime FollowedAt);
