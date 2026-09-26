using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Authorization;

public class UserAccessAuthorizationHandler
    : AuthorizationHandler<UserAccessRequirement>
{
    private readonly IUserAccessService _userAccessService;

    public UserAccessAuthorizationHandler(
        IUserAccessService userAccessService)
    {
        _userAccessService = userAccessService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserAccessRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(
                              ClaimTypes.NameIdentifier) ??
                          context.User.FindFirstValue(
                              JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdValue, out var userId))
            return;

        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;

        var decision = await _userAccessService.AuthorizeAsync(
            userId,
            requirement.Scope,
            context.User.IsInRole(UserRole.Admin.ToString()),
            cancellationToken);

        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
            return;
        }

        var failureReason = decision.DenialReason switch
        {
            AccessDenialReason.CustomerSuspended =>
                AccessFailureReasons.CustomerSuspended,
            AccessDenialReason.CustomerRequired =>
                AccessFailureReasons.CustomerRequired,
            AccessDenialReason.ShopSuspended =>
                AccessFailureReasons.ShopSuspended,
            AccessDenialReason.StaffRequired =>
                AccessFailureReasons.StaffRequired,
            AccessDenialReason.StoreOwnerRequired =>
                AccessFailureReasons.StoreOwnerRequired,
            AccessDenialReason.StoreOwnerOrStaffRequired =>
                AccessFailureReasons.StoreOwnerOrStaffRequired,
            AccessDenialReason.AdminRequired =>
                AccessFailureReasons.AdminRequired,
            _ => AccessFailureReasons.AccountInactive
        };

        context.Fail(new AuthorizationFailureReason(
            this,
            failureReason));
    }
}
