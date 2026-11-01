using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

/// <summary>
/// Store Owner management of platform-fee statements and online settlement (UC-79, UC-80, UC-81, UC-151).
/// </summary>
[ApiController]
[Route("api")]
public sealed class ShopPlatformFeeController : ControllerBase
{
    private readonly IPlatformFeeStatementService _feeStatementService;

    public ShopPlatformFeeController(IPlatformFeeStatementService feeStatementService)
    {
        _feeStatementService = feeStatementService;
    }

    /// <summary>
    /// UC-79: View monthly platform fee statements for shop (BR-092, BR-093, BR-192, BR-193).
    /// </summary>
    [HttpGet("shops/{shopId:guid}/platform-fees")]
    [StoreOwnerAnyStatusAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<PlatformFeeStatementSummaryResponse>>>> GetShopStatements(
        Guid shopId,
        [FromQuery] GetShopPlatformFeesRequest request,
        CancellationToken ct)
    {
        var result = await _feeStatementService.GetShopStatementsAsync(shopId, GetAuthenticatedUserId(), request, ct);
        return Ok(ApiResponse<PagedResult<PlatformFeeStatementSummaryResponse>>.Ok(result));
    }

    /// <summary>
    /// UC-80: View fee statement details including orders, adjustments, and payments (BR-092, BR-205).
    /// </summary>
    [HttpGet("shops/{shopId:guid}/platform-fees/{statementId:guid}")]
    [StoreOwnerAnyStatusAccess]
    public async Task<ActionResult<ApiResponse<PlatformFeeStatementDetailResponse>>> GetStatementDetail(
        Guid shopId,
        Guid statementId,
        CancellationToken ct)
    {
        var result = await _feeStatementService.GetStatementDetailAsync(statementId, shopId, GetAuthenticatedUserId(), ct);
        return Ok(ApiResponse<PlatformFeeStatementDetailResponse>.Ok(result));
    }

    /// <summary>
    /// UC-81: Pay platform fee online via PayOS (BR-092, BR-094, BR-097).
    /// </summary>
    [HttpPost("shops/{shopId:guid}/platform-fees/{statementId:guid}/payment-link")]
    [StoreOwnerAnyStatusAccess]
    public async Task<ActionResult<ApiResponse<PlatformFeePaymentLinkResponse>>> PayPlatformFee(
        Guid shopId,
        Guid statementId,
        CancellationToken ct)
    {
        var result = await _feeStatementService.CreatePaymentLinkAsync(statementId, shopId, GetAuthenticatedUserId(), ct);
        return Ok(ApiResponse<PlatformFeePaymentLinkResponse>.Ok(result));
    }

    /// <summary>
    /// UC-151: Webhook from PayOS updating platform fee payment status (BR-097, BR-167, BR-195).
    /// </summary>
    [HttpPost("platform-fees/payos/webhook")]
    [AllowAnonymous]
    [RequestSizeLimit(65536)]
    public async Task<ActionResult> FeeWebhook([FromBody] JsonElement body, CancellationToken ct)
    {
        await _feeStatementService.ProcessFeeWebhookAsync(body, ct);
        return Ok(new { code = "00", desc = "success" });
    }

    private Guid GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw AppException.Unauthorized();
    }
}
