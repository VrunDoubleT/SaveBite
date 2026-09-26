using Microsoft.AspNetCore.Authorization;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Authorization;

public sealed record UserAccessRequirement(AccessScope Scope)
    : IAuthorizationRequirement;
