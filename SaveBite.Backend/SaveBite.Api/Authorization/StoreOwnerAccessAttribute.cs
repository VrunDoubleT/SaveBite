using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class StoreOwnerAccessAttribute : AuthorizeAttribute
{
    public StoreOwnerAccessAttribute()
    {
        Policy = AuthorizationPolicies.StoreOwner;
    }
}
