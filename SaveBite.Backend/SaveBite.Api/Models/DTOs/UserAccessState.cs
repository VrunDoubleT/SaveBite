using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.DTOs;

public sealed record UserAccessState(
    UserStatus Status,
    CustomerStatus CustomerStatus,
    ShopAccessStatus ShopStatus,
    UserRole Role);
