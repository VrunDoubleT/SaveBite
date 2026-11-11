using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

/// <summary>Admin configuration of Trust Score levels and rules (UC-129 .. UC-138, UC-152 .. UC-156).</summary>
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

    // UC-129, UC-131, UC-152 (A. View Level List)
    [HttpGet("levels")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminTrustLevelResponse>>>> GetLevels(CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.GetLevelsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AdminTrustLevelResponse>>.Ok(result));
    }

    // UC-152 (A. View Level History)
    [HttpGet("levels/{levelId:guid}/history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TrustLevelHistoryItemResponse>>>> GetLevelHistory(
        Guid levelId, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.GetLevelHistoryAsync(levelId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TrustLevelHistoryItemResponse>>.Ok(result));
    }

    // UC-153 (B. Add New Level)
    [HttpPost("levels")]
    public async Task<ActionResult<ApiResponse<AdminTrustLevelResponse>>> CreateLevel(
        [FromBody] CreateTrustLevelRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.CreateLevelAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<AdminTrustLevelResponse>.Ok(result, "Trust level created successfully."));
    }

    // UC-154 (C. Update Level Information)
    [HttpPut("levels/{levelId:guid}/info")]
    public async Task<ActionResult<ApiResponse<AdminTrustLevelResponse>>> UpdateLevelInfo(
        Guid levelId, [FromBody] UpdateTrustLevelInfoRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.UpdateLevelInfoAsync(GetAuthenticatedUserId(), levelId, request, cancellationToken);
        return Ok(ApiResponse<AdminTrustLevelResponse>.Ok(result, "Trust level information updated."));
    }

    // UC-155 (D. Update Level Status)
    [HttpPut("levels/{levelId:guid}/status")]
    public async Task<ActionResult<ApiResponse<AdminTrustLevelResponse>>> UpdateLevelStatus(
        Guid levelId, [FromBody] UpdateTrustLevelStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.UpdateLevelStatusAsync(GetAuthenticatedUserId(), levelId, request, cancellationToken);
        return Ok(ApiResponse<AdminTrustLevelResponse>.Ok(result, "Trust level status updated."));
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

    // UC-156 (E. Add New Penalty Rule)
    [HttpPost("penalty-rules")]
    public async Task<ActionResult<ApiResponse<PenaltyRuleResponse>>> CreatePenaltyRule(
        [FromBody] CreatePenaltyRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.CreatePenaltyRuleAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<PenaltyRuleResponse>.Ok(result, "Penalty rule created successfully."));
    }

    // UC-134
    [HttpPut("penalty-rules/{ruleId:guid}")]
    public async Task<ActionResult<ApiResponse<PenaltyRuleResponse>>> UpdatePenaltyRule(
        Guid ruleId, [FromBody] UpdatePenaltyRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _trustConfigService.UpdatePenaltyRuleAsync(GetAuthenticatedUserId(), ruleId, request, cancellationToken);
        return Ok(ApiResponse<PenaltyRuleResponse>.Ok(result));
    }

    // Delete future scheduled penalty rule
    [HttpDelete("penalty-rules/{ruleId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeletePenaltyRule(
        Guid ruleId, CancellationToken cancellationToken)
    {
        await _trustConfigService.DeletePenaltyRuleAsync(GetAuthenticatedUserId(), ruleId, cancellationToken);
        return Ok(ApiResponse.Ok("Future penalty rule deleted successfully."));
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