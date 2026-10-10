using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class StaffActiveAccessAttribute : AuthorizeAttribute
{
    public StaffActiveAccessAttribute()
    {
        Policy = AuthorizationPolicies.StaffActive;
    }
}
