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

/// <summary>
/// Administrator management of platform-fee configuration and external payment reconciliation (UC-148, UC-149, BR-096, BR-194, BR-195).
/// </summary>
[ApiController]
[Route("api/admin")]
[AdminAccess]
public sealed class AdminPlatformFeeController : ControllerBase
{
    private readonly IPlatformFeeConfigService _feeConfigService;
    private readonly IPlatformFeeStatementService _feeStatementService;

    public AdminPlatformFeeController(
        IPlatformFeeConfigService feeConfigService,
        IPlatformFeeStatementService feeStatementService)
    {
        _feeConfigService = feeConfigService;
        _feeStatementService = feeStatementService;
    }

    /// <summary>
    /// UC-148: View active platform fee configuration (BR-189, BR-093).
    /// </summary>
    [HttpGet("platform-fee-config")]
    public async Task<ActionResult<ApiResponse<PlatformFeeConfigResponse>>> GetActiveConfig(CancellationToken ct)
    {
        var result = await _feeConfigService.GetActiveConfigAsync(ct);
        return Ok(ApiResponse<PlatformFeeConfigResponse>.Ok(result));
    }

     /// View all platform fee configurations including scheduled and past configs.
        [HttpGet("platform-fee-configs")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<PlatformFeeConfigResponse>>>> GetAllConfigs(CancellationToken ct)
        {
            var result = await _feeConfigService.GetAllConfigsAsync(ct);
            return Ok(ApiResponse<IReadOnlyList<PlatformFeeConfigResponse>>.Ok(result));
        }
        /// <summary>
        /// View platform fee statements across all shops with filters (Admin).
        /// </summary>
        [HttpGet("platform-fee-statements")]
        public async Task<ActionResult<ApiResponse<PagedResult<PlatformFeeStatementSummaryResponse>>>> GetStatements(
            [FromQuery] GetAdminPlatformFeesRequest request,
            CancellationToken ct)
        {
            var result = await _feeStatementService.GetAdminStatementsAsync(request, ct);
            return Ok(ApiResponse<PagedResult<PlatformFeeStatementSummaryResponse>>.Ok(result));
        }
        
        /// <summary>
        /// View platform fee statement details for any shop (Admin).
        /// </summary>
        [HttpGet("platform-fee-statements/{statementId:guid}")]
        public async Task<ActionResult<ApiResponse<PlatformFeeStatementDetailResponse>>> GetStatementDetail(
            Guid statementId,
            CancellationToken ct)
        {
            var result = await _feeStatementService.GetAdminStatementDetailAsync(statementId, ct);
            return Ok(ApiResponse<PlatformFeeStatementDetailResponse>.Ok(result));
        }
    /// <summary>
    /// UC-149: Update platform fee configuration (BR-189, BR-164, BR-165, BR-163).
    /// </summary>
    [HttpPut("platform-fee-config")]
    public async Task<ActionResult<ApiResponse<PlatformFeeConfigResponse>>> UpdateConfig(
        [FromBody] UpdatePlatformFeeConfigRequest request,
        CancellationToken ct)
    {
        var adminId = GetAuthenticatedUserId();
        var result = await _feeConfigService.UpdateConfigAsync(adminId, request, ct);
        return Ok(ApiResponse<PlatformFeeConfigResponse>.Ok(result));
    }

    /// <summary>
    /// Record external payment reconciliation for a fee statement (BR-096, BR-194, BR-195).
    /// </summary>
    [HttpPost("platform-fee-statements/{statementId:guid}/reconciliation")]
    public async Task<ActionResult<ApiResponse<string>>> RecordExternalPayment(
        Guid statementId,
        [FromBody] RecordExternalFeePaymentRequest request,
        CancellationToken ct)
    {
        var adminId = GetAuthenticatedUserId();
        await _feeStatementService.RecordExternalPaymentAsync(adminId, statementId, request, ct);
        return Ok(ApiResponse<string>.Ok("External payment recorded successfully and shop status updated."));
    }
    
    /// <summary>
    /// Cancel an upcoming scheduled platform fee configuration.
    /// </summary>
    [HttpDelete("platform-fee-configs/scheduled/{configId:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> CancelScheduledConfig(
        Guid configId,
        CancellationToken ct)
    {
        var adminId = GetAuthenticatedUserId();
        await _feeConfigService.CancelScheduledConfigAsync(adminId, configId, ct);
        return Ok(ApiResponse<string>.Ok("Upcoming scheduled fee configuration cancelled successfully."));
    }
    /// <summary>

    /// <summary>
    /// Trigger background check to mark overdue statements and suspend delinquent shops (BR-095, BR-193).
    /// </summary>
    [HttpPost("platform-fee-statements/process-overdue")]
    public async Task<ActionResult<ApiResponse<int>>> ProcessOverdueStatements(CancellationToken ct)
    {
        var count = await _feeStatementService.CheckAndSuspendOverdueShopsAsync(ct);
        return Ok(ApiResponse<int>.Ok(count));
    }
    
    /// <summary>
    /// Aggregate unbilled order fees into monthly fee statements for shops (BR-093, BR-192).
    /// </summary>
    [HttpPost("platform-fee-statements/generate-monthly")]
    public async Task<ActionResult<ApiResponse<int>>> GenerateMonthlyStatements(
        [FromQuery] int year,
        [FromQuery] short month,
        CancellationToken ct)
    {
        var count = await _feeStatementService.GenerateMonthlyStatementsAsync(year, month, ct);
        return Ok(ApiResponse<int>.Ok(count, $"Generated {count} monthly platform fee statement(s) for period {year}-{month}."));
    }


    private Guid GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw AppException.Unauthorized();
    }
}
