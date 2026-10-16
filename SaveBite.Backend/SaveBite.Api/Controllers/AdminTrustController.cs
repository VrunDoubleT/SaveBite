using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

/// <summary>Admin configuration of Trust Score levels and rules (UC-129 .. UC-138).</summary>
[ApiController]
[Route("api/admin/trust")]
[AdminAccess]
public sealed class AdminTrustController : ControllerBase
{
    private readonly ITrustConfigService _trustConfigService;

    public AdminTrustController(ITrustConfigService trustConfigService)
    {
        _trustConfigService = trustConfigService;
    }

    // UC-129, UC-131
    [HttpGet("levels")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminTrustLevelResponse>>>> GetLevels(CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.GetLevelsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AdminTrustLevelResponse>>.Ok(result));
    }

    // UC-130
    [HttpPut("levels/{levelId:guid}/benefits")]
    public async Task<ActionResult<ApiResponse<AdminTrustLevelResponse>>> UpdateLevelBenefits(
        Guid levelId, [FromBody] UpdateTrustLevelBenefitsRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.UpdateLevelBenefitsAsync(GetAuthenticatedUserId(), levelId, request, cancellationToken);
        return Ok(ApiResponse<AdminTrustLevelResponse>.Ok(result));
    }

    // UC-132
    [HttpPut("levels/{levelId:guid}/requirements")]
    public async Task<ActionResult<ApiResponse<AdminTrustLevelResponse>>> UpdateLevelRequirement(
        Guid levelId, [FromBody] UpdateTrustLevelRequirementRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.UpdateLevelRequirementAsync(GetAuthenticatedUserId(), levelId, request, cancellationToken);
        return Ok(ApiResponse<AdminTrustLevelResponse>.Ok(result));
    }

    // UC-133
    [HttpGet("penalty-rules")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PenaltyRuleResponse>>>> GetPenaltyRules(CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.GetPenaltyRulesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PenaltyRuleResponse>>.Ok(result));
    }

    // UC-134
    [HttpPut("penalty-rules/{ruleId:guid}")]
    public async Task<ActionResult<ApiResponse<PenaltyRuleResponse>>> UpdatePenaltyRule(
        Guid ruleId, [FromBody] UpdatePenaltyRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.UpdatePenaltyRuleAsync(GetAuthenticatedUserId(), ruleId, request, cancellationToken);
        return Ok(ApiResponse<PenaltyRuleResponse>.Ok(result));
    }

    // UC-135
    [HttpGet("score-rules")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrustScoreRuleResponse>>>> GetScoreRules(CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.GetScoreRulesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TrustScoreRuleResponse>>.Ok(result));
    }

    // UC-136
    [HttpPut("score-rules/{ruleId:guid}")]
    public async Task<ActionResult<ApiResponse<TrustScoreRuleResponse>>> UpdateScoreRule(
        Guid ruleId, [FromBody] UpdateTrustScoreRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.UpdateScoreRuleAsync(GetAuthenticatedUserId(), ruleId, request, cancellationToken);
        return Ok(ApiResponse<TrustScoreRuleResponse>.Ok(result));
    }

    // UC-137
    [HttpGet("earning-rules")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrustScoreRuleResponse>>>> GetEarningRules(CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.GetEarningRulesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TrustScoreRuleResponse>>.Ok(result));
    }

    // UC-138
    [HttpPut("earning-rules/{ruleId:guid}")]
    public async Task<ActionResult<ApiResponse<TrustScoreRuleResponse>>> UpdateEarningRule(
        Guid ruleId, [FromBody] UpdateTrustScoreRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.UpdateEarningRuleAsync(GetAuthenticatedUserId(), ruleId, request, cancellationToken);
        return Ok(ApiResponse<TrustScoreRuleResponse>.Ok(result));
    }

    private Guid GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw AppException.Unauthorized();
    }
}
