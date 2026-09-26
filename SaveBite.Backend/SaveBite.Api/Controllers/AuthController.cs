using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

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

    [AllowAnonymous]
    [HttpPost("register")]
    // Starts registration and queues an email; no user row is created yet.
    public async Task<ActionResult<ApiResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.RequestRegistrationAsync(
            request,
            cancellationToken);
        return Accepted(ApiResponse.Ok("Registration OTP sent. Use it before it expires."));
    }

    [AllowAnonymous]
    [HttpPost("register/verify")]
    // Consumes the registration OTP and creates the account. Sign-in is a separate step.
    public async Task<ActionResult<ApiResponse>> VerifyRegistration(
        VerifyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.VerifyRegistrationAsync(
            request,
            cancellationToken);
        return Ok(ApiResponse.Ok(
            "Registration completed successfully. Please sign in."));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenPairResult>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _authService.LoginAsync(
            request,
            cancellationToken);
        return Ok(ApiResponse<TokenPairResult>.Ok(tokens, "Signed in successfully."));
    }

    [AccountAccess]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> GetMe(
        CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(
            GetAuthenticatedUserId(),
            cancellationToken);
        return Ok(ApiResponse<CurrentUserResponse>.Ok(user));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<TokenPairResult>>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokens = await _refreshTokenService.RotateAsync(
            request.RefreshToken,
            cancellationToken);
        return Ok(ApiResponse<TokenPairResult>.Ok(tokens));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _refreshTokenService.RevokeAsync(
            request.RefreshToken,
            "User logout",
            cancellationToken);
        return Ok(ApiResponse.Ok("Signed out successfully."));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    // Stores the new password hash temporarily and queues an OTP email.
    public async Task<ActionResult<ApiResponse>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.RequestPasswordResetAsync(
            request,
            cancellationToken);
        return Accepted(ApiResponse.Ok(
            "If the email exists, a password-reset OTP has been sent."));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    // Verifies the OTP and applies the pending password hash from Redis.
    public async Task<ActionResult<ApiResponse>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(
            request,
            cancellationToken);
        return Ok(ApiResponse.Ok(
            "Password reset successfully. Please sign in again."));
    }

    [AccountAccess]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse>> ChangePassword(
        ChangePasswordRequest request,
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
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                          User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : throw AppException.Unauthorized();
    }
}
