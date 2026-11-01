using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

/// <summary>FE-23: Platform Dashboard & Analytics (UC-139 .. UC-144, BR-093, BR-184 .. BR-191).</summary>
[ApiController]
[Route("api/admin/analytics")]
[AdminAccess]
public sealed class AdminAnalyticsController : ControllerBase
{
    private readonly IPlatformAnalyticsService _analyticsService;

    public AdminAnalyticsController(IPlatformAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    // Unified Platform Dashboard (UC-139 .. UC-144)
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PlatformDashboardAnalyticsResponse>>> GetDashboard(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetDashboardAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformDashboardAnalyticsResponse>.Ok(result));
    }

    // UC-139: View Platform Fee Statistics (BR-093, BR-190)
    [HttpGet("platform-fees")]
    public async Task<ActionResult<ApiResponse<PlatformFeeStatisticsResponse>>> GetPlatformFeeStatistics(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetPlatformFeeStatisticsAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformFeeStatisticsResponse>.Ok(result));
    }

    // UC-140: View User Statistics (BR-184, BR-190)
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<PlatformUserStatisticsResponse>>> GetUserStatistics(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetUserStatisticsAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformUserStatisticsResponse>.Ok(result));
    }

    // UC-141: View Shop Statistics (BR-185, BR-190)
    [HttpGet("shops")]
    public async Task<ActionResult<ApiResponse<PlatformShopStatisticsResponse>>> GetShopStatistics(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetShopStatisticsAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformShopStatisticsResponse>.Ok(result));
    }

    // UC-142: View Revenue and Order Statistics (BR-190)
    [HttpGet("revenue-orders")]
    public async Task<ActionResult<ApiResponse<PlatformRevenueAndOrderStatisticsResponse>>> GetRevenueAndOrderStatistics(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetRevenueAndOrderStatisticsAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformRevenueAndOrderStatisticsResponse>.Ok(result));
    }

    // UC-143: View Platform Flash Deal Statistics (BR-190)
    [HttpGet("flash-deals")]
    public async Task<ActionResult<ApiResponse<PlatformFlashDealStatisticsResponse>>> GetFlashDealStatistics(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetFlashDealStatisticsAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformFlashDealStatisticsResponse>.Ok(result));
    }

    // UC-144: View Payment and Refund Statistics (BR-190)
    [HttpGet("payments-refunds")]
    public async Task<ActionResult<ApiResponse<PlatformPaymentAndRefundStatisticsResponse>>> GetPaymentAndRefundStatistics(
        [FromQuery] AnalyticsPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetPaymentAndRefundStatisticsAsync(request, cancellationToken);
        return Ok(ApiResponse<PlatformPaymentAndRefundStatisticsResponse>.Ok(result));
    }
}