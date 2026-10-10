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
                AuthorizationPolicies.Guest,
                new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .AddRequirements(new UserAccessRequirement(AccessScope.Guest))
                    .Build());
            options.AddPolicy(
                AuthorizationPolicies.CustomerAnyStatus,
                CreatePolicy(AccessScope.CustomerView));
            options.AddPolicy(
                AuthorizationPolicies.CustomerActive,
                CreatePolicy(AccessScope.Customer));
            options.AddPolicy(
                AuthorizationPolicies.StaffAnyStatus,
                CreatePolicy(AccessScope.StaffAnyStatus));
            options.AddPolicy(
                AuthorizationPolicies.StaffActive,
                CreatePolicy(AccessScope.Staff));
            options.AddPolicy(
                AuthorizationPolicies.StoreOwnerAnyStatus,
                CreatePolicy(AccessScope.StoreOwnerAnyStatus));
            options.AddPolicy(
                AuthorizationPolicies.StoreOwnerActive,
                CreatePolicy(AccessScope.StoreOwner));
            options.AddPolicy(
                AuthorizationPolicies.StoreOwnerOrStaffAnyStatus,
                CreatePolicy(AccessScope.ShopView));
            options.AddPolicy(
                AuthorizationPolicies.StoreOwnerOrStaffActive,
                CreatePolicy(AccessScope.StoreOwnerOrStaff));

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
                AuthorizationPolicies.CustomerOrStaffOrStoreOwner,
                CreatePolicy(AccessScope.CustomerOrStaffOrStoreOwner));
            options.AddPolicy(
                AuthorizationPolicies.CustomerView,
                CreatePolicy(AccessScope.CustomerView));
            options.AddPolicy(
                AuthorizationPolicies.ShopView,
                CreatePolicy(AccessScope.ShopView));
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
