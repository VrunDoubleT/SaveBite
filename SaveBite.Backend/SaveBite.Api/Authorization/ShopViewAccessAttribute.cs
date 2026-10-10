using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Authorization;

public sealed class ShopViewAccessAttribute : AuthorizeAttribute
{
    public ShopViewAccessAttribute()
    {
        Policy = AuthorizationPolicies.ShopView;
    }
}
