using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class StaffAnyStatusAccessAttribute : AuthorizeAttribute
{
    public StaffAnyStatusAccessAttribute()
    {
        Policy = AuthorizationPolicies.StaffAnyStatus;
    }
}
