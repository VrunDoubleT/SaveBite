using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class CustomerViewAccessAttribute : AuthorizeAttribute
{
    public CustomerViewAccessAttribute()
    {
        Policy = AuthorizationPolicies.CustomerView;
    }
}
