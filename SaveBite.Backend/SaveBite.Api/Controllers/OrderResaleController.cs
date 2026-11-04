using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Services.Implementations;
namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/orders/shop/{shopId:guid}/{orderId:guid}/resale-decision")]
[StoreOwnerOrStaffActiveAccess]
public sealed class OrderResaleController(OrderResaleService service) : ControllerBase
{
    private Guid Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw AppException.Unauthorized();
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ResaleDecisionResponse>>> Get(Guid shopId, Guid orderId, CancellationToken ct)
        => Ok(ApiResponse<ResaleDecisionResponse>.Ok(await service.GetAsync(Actor, shopId, orderId, ct)));
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ResaleDecisionResponse>>> Decide(Guid shopId, Guid orderId, ResaleDecisionRequest request, CancellationToken ct)
        => Ok(ApiResponse<ResaleDecisionResponse>.Ok(await service.DecideAsync(Actor, shopId, orderId, request, ct)));
}
