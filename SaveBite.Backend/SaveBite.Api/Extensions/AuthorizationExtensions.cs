using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAccessAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = CreatePolicy(AccessScope.Account);
            options.AddPolicy(
                AuthorizationPolicies.JwtOnly,
                new AuthorizationPolicyBuilder(
                        JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build());
            options.AddPolicy(
                AuthorizationPolicies.AccountOnly,
                CreatePolicy(AccessScope.Account));
            options.AddPolicy(
                AuthorizationPolicies.Customer,
                CreatePolicy(AccessScope.Customer));
            options.AddPolicy(
                AuthorizationPolicies.Staff,
                CreatePolicy(AccessScope.Staff));
            options.AddPolicy(
                AuthorizationPolicies.StoreOwner,
                CreatePolicy(AccessScope.StoreOwner));
            options.AddPolicy(
                AuthorizationPolicies.StoreOwnerOrStaff,
                CreatePolicy(AccessScope.StoreOwnerOrStaff));
            options.AddPolicy(
                AuthorizationPolicies.Admin,
                CreatePolicy(AccessScope.Admin));
        });

        services.AddScoped<IAuthorizationHandler, UserAccessAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationMiddlewareResultHandler>();

        return services;
    }

    private static AuthorizationPolicy CreatePolicy(AccessScope scope)
        => new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .AddRequirements(new UserAccessRequirement(scope))
            .Build();
}
