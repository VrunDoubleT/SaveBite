using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class StoreOwnerOrStaffActiveAccessAttribute : AuthorizeAttribute
{
    public StoreOwnerOrStaffActiveAccessAttribute()
    {
        Policy = AuthorizationPolicies.StoreOwnerOrStaffActive;
    }
}
