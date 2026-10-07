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
[Route("api/owner/shops")]
public class OwnerShopStaffController(IShopStaffService staffService) : ControllerBase
{
    
    [HttpGet]
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
    
    [HttpGet("staff-candidates")]
    public async Task<IActionResult> SearchStaffCandidates(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await staffService.SearchStaffCandidatesAsync(
            GetCurrentUserId(),
            keyword,
            page,
            pageSize,
            cancellationToken
        );

        return Ok(
            ApiResponse<PagedResult<StaffCandidateResponse>>.Ok(
                result,
                "Staff candidates retrieved successfully"
            )
        );
    }

    [HttpPost("invitations")]
    public async Task<IActionResult> InviteStaff(
        [FromBody] InviteStaffRequest request,
        CancellationToken cancellationToken)
    {
        var result = await staffService.InviteStaffAsync(
            GetCurrentUserId(),
            request,
            cancellationToken
        );

        return Ok(
            ApiResponse<StaffInvitationResponse>.Ok(
                result,
                "Staff invitation sent successfully"
            )
        );
    }
    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await staffService.GetShopInvitationsAsync(
            GetCurrentUserId(),
            page,
            pageSize,
            cancellationToken
        );

        return Ok(
            ApiResponse<PagedResult<StaffInvitationResponse>>.Ok(
                result,
                "Invitations retrieved successfully"
            )
        );
    }

    [HttpDelete("invitations/{invitationId:guid}")]
    public async Task<IActionResult> RevokeInvitation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        await staffService.RevokeInvitationAsync(
            GetCurrentUserId(),
            invitationId,
            cancellationToken
        );

        return Ok(
            ApiResponse.Ok(
                "Invitation revoked successfully"
            )
        );
    }


    [HttpGet("staffs")]
    public async Task<IActionResult> GetStaffs(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await staffService.GetShopStaffsAsync(
            GetCurrentUserId(),
            keyword,
            page,
            pageSize,
            cancellationToken
        );

        return Ok(
            ApiResponse<PagedResult<ShopStaffResponse>>.Ok(
                result,
                "Staffs retrieved successfully"
            )
        );
    }

    [HttpPut("staffs/{staffId:guid}")]
    public async Task<IActionResult> UpdateStaff(
        Guid staffId,
        [FromBody] UpdateStaffInfoRequest request,
        CancellationToken cancellationToken)
    {
        await staffService.UpdateStaffInfoAsync(
            GetCurrentUserId(),
            staffId,
            request,
            cancellationToken
        );

        return Ok(
            ApiResponse.Ok("Staff updated successfully")
        );
    }

    [HttpDelete("staffs/{staffId:guid}")]
    public async Task<IActionResult> RemoveStaff(
        Guid staffId,
        CancellationToken cancellationToken)
    {
        await staffService.RemoveStaffAsync(
            GetCurrentUserId(),
            staffId,
            cancellationToken
        );

        return Ok(
            ApiResponse.Ok("Staff removed successfully")
        );
    }

    [HttpGet("staffs/{staffId:guid}/activity-logs")]
    public async Task<IActionResult> GetStaffActivityLogs(
        Guid staffId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await staffService.GetStaffActivityLogsAsync(
            GetCurrentUserId(),
            staffId,
            page,
            pageSize,
            cancellationToken
        );

        return Ok(
            ApiResponse<PagedResult<StaffActivityLogResponse>>.Ok(
                result,
                "Activity logs retrieved successfully"
            )
        );
    }
    

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id)
            ? id
            : throw AppException.Unauthorized();
    }
}