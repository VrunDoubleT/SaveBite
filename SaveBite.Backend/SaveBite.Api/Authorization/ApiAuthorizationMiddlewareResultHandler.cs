using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Authorization;

public class ApiAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler
        _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await _defaultHandler.HandleAsync(
                next,
                context,
                policy,
                authorizeResult);
            return;
        }

        if (context.Response.HasStarted)
            return;

        var failureCode = authorizeResult.AuthorizationFailure?
            .FailureReasons
            .Select(x => x.Message)
            .FirstOrDefault();

        var (statusCode, message, errorCode) =
            authorizeResult.Challenged
                ? (
                    HttpStatusCode.Unauthorized,
                    "Authentication is required.",
                    ErrorCodes.Unauthorized)
                : MapForbiddenFailure(failureCode);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(
            ApiResponse.Fail(message, errorCode),
            cancellationToken: context.RequestAborted);
    }

    private static (
        HttpStatusCode StatusCode,
        string Message,
        string ErrorCode) MapForbiddenFailure(string? failureCode)
        => failureCode switch
        {
            AccessFailureReasons.AccountInactive => (
                HttpStatusCode.Forbidden,
                "This account is suspended.",
                ErrorCodes.AccountInactive),
            AccessFailureReasons.CustomerSuspended => (
                HttpStatusCode.Forbidden,
                "Customer features are suspended for this account.",
                ErrorCodes.CustomerSuspended),
            AccessFailureReasons.CustomerRequired => (
                HttpStatusCode.Forbidden,
                "Customer access is required.",
                ErrorCodes.CustomerRequired),
            AccessFailureReasons.ShopSuspended => (
                HttpStatusCode.Forbidden,
                "Shop features are suspended for this account.",
                ErrorCodes.ShopSuspended),
            AccessFailureReasons.StoreOwnerRequired => (
                HttpStatusCode.Forbidden,
                "Store owner access is required.",
                ErrorCodes.StoreOwnerRequired),
            AccessFailureReasons.StaffRequired => (
                HttpStatusCode.Forbidden,
                "An active staff membership is required.",
                ErrorCodes.StaffRequired),
            AccessFailureReasons.StoreOwnerOrStaffRequired => (
                HttpStatusCode.Forbidden,
                "An active store owner or staff membership is required.",
                ErrorCodes.StoreOwnerOrStaffRequired),
            AccessFailureReasons.AdminRequired => (
                HttpStatusCode.Forbidden,
                "Administrator access is required.",
                ErrorCodes.AdminRequired),
            _ => (
                HttpStatusCode.Forbidden,
                "You do not have permission to access this resource.",
                ErrorCodes.Forbidden)
        };
}
