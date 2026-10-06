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

    [HttpPost("{id:guid}/suspend")]
    public async Task<ActionResult<ApiResponse>> SuspendAccount(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        await _adminUserService.SuspendAccountAsync(adminId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok("User account suspended successfully."));
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<ApiResponse>> ReactivateAccount(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        await _adminUserService.ReactivateAccountAsync(adminId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok("User account reactivated successfully."));
    }

    [HttpPost("{id:guid}/customer-status/suspend")]
    public async Task<ActionResult<ApiResponse>> SuspendCustomer(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        await _adminUserService.SuspendCustomerAsync(adminId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Customer profile suspended successfully."));
    }

    [HttpPost("{id:guid}/customer-status/reactivate")]
    public async Task<ActionResult<ApiResponse>> ReactivateCustomer(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = GetAdminId();
        await _adminUserService.ReactivateCustomerAsync(adminId, id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Customer profile reactivated successfully."));
    }
}