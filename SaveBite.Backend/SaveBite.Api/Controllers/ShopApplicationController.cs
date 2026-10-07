using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/shop-applications")]
[AccountAccess]
public sealed class ShopApplicationController : ControllerBase
{
    private readonly IShopApplicationService _service;
    public ShopApplicationController(IShopApplicationService service) => _service = service;

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<ShopApplicationResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var application = await _service.GetMyApplicationAsync(GetAuthenticatedUserId(), cancellationToken);
        if (application is null) return NotFound(ApiResponse<ShopApplicationResponse>.Fail("No shop application was found."));
        return Ok(ApiResponse<ShopApplicationResponse>.Ok(application, "Shop application retrieved successfully."));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ShopApplicationResponse>>> Create(
        [FromForm] CreateShopApplicationRequest request,
        [FromForm] IFormFile? logo,
        [FromForm] IFormFile? coverImage,
        [FromForm] List<IFormFile>? documents,
        [FromForm] List<string>? documentTypes,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(GetAuthenticatedUserId(), request, logo, coverImage, documents ?? [], documentTypes ?? [], cancellationToken);
        return Ok(ApiResponse<ShopApplicationResponse>.Ok(result, "Shop application submitted successfully."));
    }

    [HttpPut("{applicationId:guid}/resubmit")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ShopApplicationResponse>>> Resubmit(
        Guid applicationId,
        [FromForm] ResubmitShopApplicationRequest request,
        [FromForm] IFormFile? logo,
        [FromForm] IFormFile? coverImage,
        [FromForm] List<IFormFile>? documents,
        [FromForm] List<string>? documentTypes,
        CancellationToken cancellationToken)
    {
        request.ApplicationId = applicationId;
        var result = await _service.ResubmitAsync(GetAuthenticatedUserId(), request, logo, coverImage, documents ?? [], documentTypes ?? [], cancellationToken);
        return Ok(ApiResponse<ShopApplicationResponse>.Ok(result, "Shop application resubmitted successfully."));
    }

    [HttpPost("{applicationId:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid applicationId, CancellationToken cancellationToken)
    {
        await _service.CancelAsync(GetAuthenticatedUserId(), applicationId, cancellationToken);
        return Ok(ApiResponse.Ok("Shop application cancelled successfully."));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out var userId) ? userId : throw AppException.Unauthorized();
    }
}
