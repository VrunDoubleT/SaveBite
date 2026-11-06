using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Constants;
namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/orders/shop/{shopId:guid}/{orderId:guid}/resale-decision")]
[StoreOwnerOrStaffActiveAccess]
public sealed class OrderResaleController(OrderResaleService service, IShopOrderEventService events, ILogger<OrderResaleController> logger) : ControllerBase
{
    private Guid Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw AppException.Unauthorized();
    [HttpGet("/api/orders/shop/{shopId:guid}/restock")]
    public async Task<ActionResult<ApiResponse<PagedResult<RestockTaskResponse>>>> List(Guid shopId, bool pendingOnly = true,
        int page = 1, int pageSize = 10, CancellationToken ct = default, Guid? flashDealId = null)
        => Ok(ApiResponse<PagedResult<RestockTaskResponse>>.Ok(await service.ListAsync(Actor, shopId, pendingOnly, page, pageSize, ct, flashDealId)));

    [HttpGet("/api/orders/shop/{shopId:guid}/restock-summary")]
    public async Task<ActionResult<ApiResponse<RestockSummaryResponse>>> Summary(Guid shopId, CancellationToken ct)
        => Ok(ApiResponse<RestockSummaryResponse>.Ok(await service.SummaryAsync(Actor, shopId, ct)));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ResaleDecisionResponse>>> Get(Guid shopId, Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<ResaleDecisionResponse>.Ok(await service.GetAsync(Actor, shopId, orderId, ct)));
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ResaleDecisionResponse>>> Decide(Guid shopId, Guid orderId, ResaleDecisionRequest request, CancellationToken ct)
    {
        var result = await service.DecideAsync(Actor, shopId, orderId, request, ct);
        try { await events.NotifyAsync(shopId, orderId, RealtimeEvents.OrderReasons.RestockChanged, ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        { logger.LogWarning(ex, "Could not notify restock decision for order {OrderId}.", orderId); }
        return Ok(ApiResponse<ResaleDecisionResponse>.Ok(result));
    }
}
