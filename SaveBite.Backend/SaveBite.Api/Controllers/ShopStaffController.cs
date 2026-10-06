using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

/// <summary>Store owner side: manage invitations and staff of one shop.</summary>
[ApiController]
[Authorize]
[StoreOwnerAccess]
[Route("api/owner/shops/{shopId:guid}")]
public class OwnerShopStaffController(IShopStaffService staffService) : ControllerBase
{
    
    [HttpGet("~/api/owner/shops/me")]
    public async Task<IActionResult> GetMyShop(
        CancellationToken cancellationToken)
    {
        var result = await staffService.GetOwnerShopAsync(
            GetCurrentUserId(),
            cancellationToken
        );

        return Ok(
            ApiResponse<OwnerShopResponse>.Ok(
                result,
                "Shop retrieved successfully"
            )
        );
    }
    [HttpPost("invitations")]
    public async Task<IActionResult> InviteStaff(
        Guid shopId, [FromBody] InviteStaffRequest request, CancellationToken cancellationToken)
    {
        var result = await staffService.InviteStaffAsync(GetCurrentUserId(), shopId, request, cancellationToken);
        return Ok(ApiResponse<StaffInvitationResponse>.Ok(result, "Staff invitation sent successfully"));
    }

    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitations(Guid shopId, CancellationToken cancellationToken)
    {
        var result = await staffService.GetShopInvitationsAsync(GetCurrentUserId(), shopId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<StaffInvitationResponse>>.Ok(result, "Invitations retrieved successfully"));
    }

    [HttpDelete("invitations/{invitationId:guid}")]
    public async Task<IActionResult> RevokeInvitation(Guid shopId, Guid invitationId, CancellationToken cancellationToken)
    {
        await staffService.RevokeInvitationAsync(GetCurrentUserId(), shopId, invitationId, cancellationToken);
        return Ok(ApiResponse.Ok("Invitation revoked successfully"));
    }

    [HttpGet("staffs")]
    public async Task<IActionResult> GetStaffs(Guid shopId, CancellationToken cancellationToken)
    {
        var result = await staffService.GetShopStaffsAsync(GetCurrentUserId(), shopId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<ShopStaffResponse>>.Ok(result, "Staffs retrieved successfully"));
    }

    [HttpPut("staffs/{staffId:guid}")]
    public async Task<IActionResult> UpdateStaff(
        Guid shopId, Guid staffId, [FromBody] UpdateStaffInfoRequest request, CancellationToken cancellationToken)
    {
        await staffService.UpdateStaffInfoAsync(GetCurrentUserId(), shopId, staffId, request, cancellationToken);
        return Ok(ApiResponse.Ok("Staff updated successfully"));
    }

    [HttpDelete("staffs/{staffId:guid}")]
    public async Task<IActionResult> RemoveStaff(Guid shopId, Guid staffId, CancellationToken cancellationToken)
    {
        await staffService.RemoveStaffAsync(GetCurrentUserId(), shopId, staffId, cancellationToken);
        return Ok(ApiResponse.Ok("Staff removed successfully"));
    }

    [HttpGet("staffs/{staffId:guid}/activity-logs")]
    public async Task<IActionResult> GetStaffActivityLogs(Guid shopId, Guid staffId, CancellationToken cancellationToken)
    {
        var result = await staffService.GetStaffActivityLogsAsync(GetCurrentUserId(), shopId, staffId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<StaffActivityLogResponse>>.Ok(result, "Activity logs retrieved successfully"));
    }
    

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}