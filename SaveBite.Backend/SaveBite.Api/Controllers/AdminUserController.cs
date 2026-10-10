using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "Admin")]
public class AdminUserController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUserController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    private Guid GetAdminId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idClaim, out var adminId)) return adminId;
        throw new UnauthorizedAccessException("Admin ID not found in token.");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserSummaryResponse>>>> GetUsers(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.GetUsersAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<UserSummaryResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDetailsResponse>>> GetUserDetails(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _adminUserService.GetUserDetailsAsync(id, cancellationToken);
        return Ok(ApiResponse<UserDetailsResponse>.Ok(result));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();

        await _adminUserService.UpdateStatusAsync(adminId, id, request, cancellationToken);

        return Ok(ApiResponse.Ok("User status updated successfully."));
    }
}