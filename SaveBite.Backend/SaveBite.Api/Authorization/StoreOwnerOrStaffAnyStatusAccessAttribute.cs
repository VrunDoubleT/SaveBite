using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class StoreOwnerOrStaffAnyStatusAccessAttribute : AuthorizeAttribute
{
    public StoreOwnerOrStaffAnyStatusAccessAttribute()
    {
        Policy = AuthorizationPolicies.StoreOwnerOrStaffAnyStatus;
    }
}
