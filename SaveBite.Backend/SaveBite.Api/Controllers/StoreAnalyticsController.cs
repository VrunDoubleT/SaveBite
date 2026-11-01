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

/// <summary>FE-17: Store Dashboard & Analytics (UC-97 .. UC-100, BR-113 .. BR-117).</summary>
[ApiController]
[Route("api/shops/me/analytics")]
[StoreOwnerAnyStatusAccess]
public sealed class StoreAnalyticsController : ControllerBase
{
    private readonly IStoreAnalyticsService _analyticsService;

    public StoreAnalyticsController(IStoreAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    // Unified Store Dashboard (UC-97, UC-98, UC-99, UC-100)
    [HttpGet]
    public async Task<ActionResult<ApiResponse<StoreDashboardAnalyticsResponse>>> GetDashboard(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetDashboardAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<StoreDashboardAnalyticsResponse>.Ok(result));
    }

    // UC-97: View Revenue Overview (BR-113, BR-114)
    [HttpGet("revenue-overview")]
    public async Task<ActionResult<ApiResponse<StoreRevenueOverviewResponse>>> GetRevenueOverview(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetRevenueOverviewAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<StoreRevenueOverviewResponse>.Ok(result));
    }

    // UC-98: View Order Statistics (BR-113, BR-115)
    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<StoreOrderStatisticsResponse>>> GetOrderStatistics(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetOrderStatisticsAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<StoreOrderStatisticsResponse>.Ok(result));
    }

    // UC-99: View Shop Flash Deal Statistics (BR-113, BR-116)
    [HttpGet("flash-deals")]
    public async Task<ActionResult<ApiResponse<StoreFlashDealStatisticsResponse>>> GetFlashDealStatistics(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetFlashDealStatisticsAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<StoreFlashDealStatisticsResponse>.Ok(result));
    }

    // UC-100: View Revenue by Payment Method (BR-113, BR-117)
    [HttpGet("payment-methods")]
    public async Task<ActionResult<ApiResponse<StorePaymentMethodRevenueResponse>>> GetRevenueByPaymentMethod(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetRevenueByPaymentMethodAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<StorePaymentMethodRevenueResponse>.Ok(result));
    }

    private Guid GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw AppException.Unauthorized();
    }
}