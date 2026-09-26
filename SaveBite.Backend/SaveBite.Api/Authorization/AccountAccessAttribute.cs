using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class AccountAccessAttribute : AuthorizeAttribute
{
    public AccountAccessAttribute()
    {
        Policy = AuthorizationPolicies.AccountOnly;
    }
}
