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
public sealed class OrderResaleController(OrderResaleService service) : ControllerBase
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
    [StoreOwnerOrStaffAnyStatusAccess]
    public async Task<ActionResult<ApiResponse<ResaleDecisionResponse>>> Get(Guid shopId, Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<ResaleDecisionResponse>.Ok(await service.GetAsync(Actor, shopId, orderId, ct)));
    [HttpPost]
    [StoreOwnerOrStaffActiveAccess]
    public async Task<ActionResult<ApiResponse<ResaleDecisionResponse>>> Decide(Guid shopId, Guid orderId, ResaleDecisionRequest request, CancellationToken ct)
        => Ok(ApiResponse<ResaleDecisionResponse>.Ok(await service.DecideAsync(Actor, shopId, orderId, request, ct)));
}
