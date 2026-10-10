using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class CustomerOrStaffOrStoreOwnerAccessAttribute : AuthorizeAttribute
{
    public CustomerOrStaffOrStoreOwnerAccessAttribute()
    {
        Policy = AuthorizationPolicies.CustomerOrStaffOrStoreOwner;
    }
}
