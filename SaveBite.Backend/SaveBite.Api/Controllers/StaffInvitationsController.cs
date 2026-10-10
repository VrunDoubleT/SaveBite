using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

// Manage staff invitation candidates, sending, responses, and revocation.
[ApiController]
[Route("api/staff-invitations")]
public sealed class StaffInvitationsController : ControllerBase
{
    private readonly IStaffInvitationService _invitationService;

    public StaffInvitationsController(IStaffInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpGet("me")]
    [CustomerAnyStatusAccess]
    public async Task<IActionResult> GetMyInvitations(CancellationToken cancellationToken)
    {
        var result = await _invitationService.GetCustomerInvitationsAsync(GetAuthenticatedUserId(), cancellationToken);
        return Ok(ApiResponse<IEnumerable<StaffInvitationResponse>>.Ok(result, "Invitations retrieved successfully"));
    }

    [HttpPost("{invitationId:guid}/acceptances")]
    [CustomerAccess]
    public async Task<IActionResult> Accept(Guid invitationId, CancellationToken cancellationToken)
    {
        await _invitationService.AcceptInvitationAsync(GetAuthenticatedUserId(), invitationId, cancellationToken);
        return Ok(ApiResponse.Ok("Invitation accepted"));
    }

    [HttpPost("{invitationId:guid}/rejections")]
    [CustomerAccess]
    public async Task<IActionResult> Decline(Guid invitationId, CancellationToken cancellationToken)
    {
        await _invitationService.DeclineInvitationAsync(GetAuthenticatedUserId(), invitationId, cancellationToken);
        return Ok(ApiResponse.Ok("Invitation declined"));
    }

    [HttpGet("candidates")]
    [CustomerAnyStatusAccess]
    public async Task<IActionResult> SearchStaffCandidates(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _invitationService.SearchStaffCandidatesAsync(
            GetAuthenticatedUserId(),
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

    [HttpPost]
    [StoreOwnerAccess]
    public async Task<IActionResult> InviteStaff(
        [FromBody] InviteStaffRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _invitationService.InviteStaffAsync(
            GetAuthenticatedUserId(),
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

    [HttpGet("shop")]
    [StoreOwnerAnyStatusAccess]
    public async Task<IActionResult> GetInvitations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _invitationService.GetShopInvitationsAsync(
            GetAuthenticatedUserId(),
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

    [HttpDelete("{invitationId:guid}")]
    [StoreOwnerAccess]
    public async Task<IActionResult> RevokeInvitation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        await _invitationService.RevokeInvitationAsync(
            GetAuthenticatedUserId(),
            invitationId,
            cancellationToken
        );

        return Ok(
            ApiResponse.Ok(
                "Invitation revoked successfully"
            )
        );
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : throw AppException.Unauthorized();
    }
}
