using SaveBite.Backend.Models.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

// Manage user profiles and account administration.
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPut("me")]
    // Update the current user profile.
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> UpdateProfile([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _userService.UpdateProfileAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<CurrentUserResponse>.Ok(profile, "Profile updated successfully."));
    }

    [HttpPut("me/avatar")]
    // Upload the current user avatar.
    [CustomerAccess]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> UploadAvatar([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw AppException.BadRequest("Avatar file is required.");

        var profile = await _userService.UploadAvatarAsync(GetAuthenticatedUserId(), file, cancellationToken);
        return Ok(ApiResponse<CurrentUserResponse>.Ok(profile, "Avatar updated successfully."));
    }

    [HttpGet("me")]
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> GetMe(
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetCurrentUserAsync(
            GetAuthenticatedUserId(),
            cancellationToken);
        return Ok(ApiResponse<CurrentUserResponse>.Ok(user, "Get profile successfully."));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out var userId) ? userId : throw AppException.Unauthorized();
    }

    [HttpGet]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<UserSummaryResponse>>>> GetUsers(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<UserSummaryResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<UserDetailsResponse>>> GetUserDetails(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserDetailsAsync(id, cancellationToken);
        return Ok(ApiResponse<UserDetailsResponse>.Ok(result));
    }

    [HttpPatch("{id:guid}/status")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAuthenticatedUserId();

        await _userService.UpdateStatusAsync(adminId, id, request, cancellationToken);

        return Ok(ApiResponse.Ok("User status updated successfully."));
    }                   
}
