using SaveBite.Backend.Models.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace SaveBite.Backend.Controllers;

// Manage authentication, credentials, and sessions.
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthController(
        IAuthService authService,
        IRefreshTokenService refreshTokenService)
    {
        _authService = authService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpGet("me")]
    [AccountAccess]
    // Retrieve the authenticated account for session verification.
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> GetMe(
        CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(
            GetAuthenticatedUserId(),
            cancellationToken);
        return Ok(ApiResponse<CurrentUserResponse>.Ok(user));
    }

    [HttpPost("registrations")]
    [AllowAnonymous]
    // Starts registration and queues an email; no user row is created yet.
    public async Task<ActionResult<ApiResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.RequestRegistrationAsync(
            request,
            cancellationToken);
        return Accepted(ApiResponse.Ok("Registration OTP sent. Use it before it expires."));
    }

    [HttpPost("registration-verifications")]
    [AllowAnonymous]
    // Consumes the registration OTP and creates the account. Sign-in is a separate step.
    public async Task<ActionResult<ApiResponse>> VerifyRegistration(
        [FromBody] VerifyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.VerifyRegistrationAsync(
            request,
            cancellationToken);
        return Ok(ApiResponse.Ok(
            "Registration completed successfully. Please sign in."));
    }

    [HttpPost("sessions")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenPairResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _authService.LoginAsync(
            request,
            cancellationToken);
        return Ok(ApiResponse<TokenPairResponse>.Ok(tokens, "Signed in successfully."));
    }

    [HttpPost("token-renewals")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TokenPairResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _refreshTokenService.RotateAsync(
            request.RefreshToken,
            cancellationToken);
        return Ok(ApiResponse<TokenPairResponse>.Ok(tokens));
    }

    [HttpPost("session-revocations")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse>> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _refreshTokenService.RevokeAsync(
            request.RefreshToken,
            "User logout",
            cancellationToken);
        return Ok(ApiResponse.Ok("Signed out successfully."));
    }

    [HttpPost("password-resets")]
    [AllowAnonymous]
    // Stores the new password hash temporarily and queues an OTP email.
    public async Task<ActionResult<ApiResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.RequestPasswordResetAsync(
            request,
            cancellationToken);
        return Accepted(ApiResponse.Ok(
            "If the email exists, a password-reset OTP has been sent."));
    }

    [HttpPost("password-reset-verifications")]
    [AllowAnonymous]
    // Verifies the OTP and applies the pending password hash from Redis.
    public async Task<ActionResult<ApiResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(
            request,
            cancellationToken);
        return Ok(ApiResponse.Ok(
            "Password reset successfully. Please sign in again."));
    }
    [HttpPut("me/password")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(
            GetAuthenticatedUserId(),
            request,
            cancellationToken);
        return Ok(ApiResponse.Ok(
            "Password changed successfully. Please sign in again."));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out var userId) ? userId : throw AppException.Unauthorized();
    }

}
