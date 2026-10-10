using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class StoreOwnerActiveAccessAttribute : AuthorizeAttribute
{
    public StoreOwnerActiveAccessAttribute()
    {
        Policy = AuthorizationPolicies.StoreOwnerActive;
    }
}
