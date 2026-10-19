using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

/// <summary>Customer-facing Trust Score endpoints (UC-44 .. UC-49).</summary>
[ApiController]
[Route("api/trust-score")]
[CustomerAnyStatusAccess]
public sealed class TrustScoreController : ControllerBase
{
    private readonly ITrustScoreService _trustScoreService;

    public TrustScoreController(ITrustScoreService trustScoreService)
    {
        _trustScoreService = trustScoreService;
    }

    // UC-44
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<TrustScoreOverviewResponse>>> GetMyTrustScore(CancellationToken cancellationToken)
    {
        var result = await _trustScoreService.GetOverviewAsync(GetAuthenticatedUserId(), cancellationToken);
        return Ok(ApiResponse<TrustScoreOverviewResponse>.Ok(result));
    }

    // UC-45
    [HttpGet("me/level-progress")]
    public async Task<ActionResult<ApiResponse<LevelProgressResponse>>> GetMyLevelProgress(CancellationToken cancellationToken)
    {
        var result = await _trustScoreService.GetLevelProgressAsync(GetAuthenticatedUserId(), cancellationToken);
        return Ok(ApiResponse<LevelProgressResponse>.Ok(result));
    }

    // UC-46
    [HttpGet("me/history")]
    public async Task<ActionResult<ApiResponse<PagedResult<TrustScoreHistoryItemResponse>>>> GetMyHistory(
        [FromQuery] GetTrustScoreHistoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustScoreService.GetHistoryAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<PagedResult<TrustScoreHistoryItemResponse>>.Ok(result));
    }

    // UC-47
    [HttpGet("levels")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrustLevelInfoResponse>>>> GetLevels(CancellationToken cancellationToken)
    {
        var result = await _trustScoreService.GetLevelsAsync(GetAuthenticatedUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TrustLevelInfoResponse>>.Ok(result));
    }

    // UC-48
    [HttpGet("me/penalties")]
    public async Task<ActionResult<ApiResponse<PagedResult<PenaltyItemResponse>>>> GetMyPenalties(
        [FromQuery] GetPenaltiesRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustScoreService.GetPenaltiesAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<PagedResult<PenaltyItemResponse>>.Ok(result));
    }

    // UC-49
    [HttpGet("me/penalties/{penaltyId:guid}/recovery")]
    public async Task<ActionResult<ApiResponse<PenaltyRecoveryResponse>>> GetMyPenaltyRecovery(
        Guid penaltyId, CancellationToken cancellationToken)
    {
        var result = await _trustScoreService.GetPenaltyRecoveryAsync(GetAuthenticatedUserId(), penaltyId, cancellationToken);
        return Ok(ApiResponse<PenaltyRecoveryResponse>.Ok(result));
    }

    private Guid GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw AppException.Unauthorized();
    }
}
