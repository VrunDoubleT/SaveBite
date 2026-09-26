using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.DTOs;

public sealed record UserAccessDecision(
    bool IsAllowed,
    AccessDenialReason? DenialReason)
{
    public static UserAccessDecision Allow() => new(true, null);

    public static UserAccessDecision Deny(AccessDenialReason reason)
        => new(false, reason);
}
