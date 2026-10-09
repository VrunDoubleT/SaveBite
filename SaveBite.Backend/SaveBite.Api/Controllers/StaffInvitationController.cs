using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

/// <summary>Customer + staff side: my invitations, my associated shops.</summary>
[ApiController]
[Authorize]
[Route("api/staff")]
public class StaffInvitationController(IShopStaffService staffService) : ControllerBase
{
    [HttpGet("invitations")]
    public async Task<IActionResult> GetMyInvitations(CancellationToken cancellationToken)
    {
        var result = await staffService.GetCustomerInvitationsAsync(GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse<IEnumerable<StaffInvitationResponse>>.Ok(result, "Invitations retrieved successfully"));
    }

    [HttpPost("invitations/{invitationId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid invitationId, CancellationToken cancellationToken)
    {
        await staffService.AcceptInvitationAsync(GetCurrentUserId(), invitationId, cancellationToken);
        return Ok(ApiResponse.Ok("Invitation accepted"));
    }

    [HttpPost("invitations/{invitationId:guid}/decline")]
    public async Task<IActionResult> Decline(Guid invitationId, CancellationToken cancellationToken)
    {
        await staffService.DeclineInvitationAsync(GetCurrentUserId(), invitationId, cancellationToken);
        return Ok(ApiResponse.Ok("Invitation declined"));
    }

    [HttpGet("shops")]
    public async Task<IActionResult> GetAssociatedShops(
        CancellationToken cancellationToken)
    {
        var result = await staffService
            .GetAssociatedShopsAsync(
                GetCurrentUserId(),
                cancellationToken);

        var message = result.Any()
            ? "Shops retrieved successfully"
            : "No associated shops found";

        return Ok(
            ApiResponse<IEnumerable<AssociatedShopResponse>>
                .Ok(result, message)
        );
    }

    [HttpGet("shops/{shopId:guid}")]
    public async Task<IActionResult> GetShopAndStaffInfo(
        Guid shopId,
        CancellationToken cancellationToken)
    {
        var result = await staffService.GetShopAndStaffInfoAsync(
            GetCurrentUserId(),
            shopId,
            cancellationToken);

        return Ok(
            ApiResponse<StaffShopInfoResponse>
                .Ok(
                    result,
                    "Shop and staff information retrieved successfully"
                )
        );
    }
    
    [HttpDelete("shops/{shopId:guid}")]
    public async Task<IActionResult> LeaveShop(
        Guid shopId,
        CancellationToken cancellationToken)
    {
        await staffService.LeaveShopAsync(
            GetCurrentUserId(),
            shopId,
            cancellationToken
        );

        return Ok(
            ApiResponse.Ok(
                "You have left the shop successfully"
            )
        );
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw AppException.Unauthorized();
    }
}