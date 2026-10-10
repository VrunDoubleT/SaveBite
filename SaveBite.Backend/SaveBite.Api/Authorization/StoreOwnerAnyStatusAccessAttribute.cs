using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class StoreOwnerAnyStatusAccessAttribute : AuthorizeAttribute
{
    public StoreOwnerAnyStatusAccessAttribute()
    {
        Policy = AuthorizationPolicies.StoreOwnerAnyStatus;
    }
}
