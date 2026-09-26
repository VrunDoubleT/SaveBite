using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class CustomerAccessAttribute : AuthorizeAttribute
{
    public CustomerAccessAttribute()
    {
        Policy = AuthorizationPolicies.Customer;
    }
}
