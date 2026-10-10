using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class CustomerAnyStatusAccessAttribute : AuthorizeAttribute
{
    public CustomerAnyStatusAccessAttribute()
    {
        Policy = AuthorizationPolicies.CustomerAnyStatus;
    }
}
