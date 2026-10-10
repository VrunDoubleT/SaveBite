using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class GuestAccessAttribute : AuthorizeAttribute
{
    public GuestAccessAttribute()
    {
        Policy = AuthorizationPolicies.Guest;
    }
}
