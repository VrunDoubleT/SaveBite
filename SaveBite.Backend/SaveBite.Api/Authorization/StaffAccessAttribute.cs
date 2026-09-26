using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class StaffAccessAttribute : AuthorizeAttribute
{
    public StaffAccessAttribute()
    {
        Policy = AuthorizationPolicies.Staff;
    }
}
