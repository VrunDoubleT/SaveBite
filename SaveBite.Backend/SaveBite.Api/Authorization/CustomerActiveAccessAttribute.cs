using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class CustomerActiveAccessAttribute : AuthorizeAttribute
{
    public CustomerActiveAccessAttribute()
    {
        Policy = AuthorizationPolicies.CustomerActive;
    }
}
