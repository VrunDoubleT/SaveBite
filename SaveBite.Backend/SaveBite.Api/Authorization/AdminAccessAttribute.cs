using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class AdminAccessAttribute : AuthorizeAttribute
{
    public AdminAccessAttribute()
    {
        Policy = AuthorizationPolicies.Admin;
    }
}
