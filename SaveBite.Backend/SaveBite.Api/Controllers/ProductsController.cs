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
[StoreOwnerAccess]
[Route("api/store-owner/products")]
[RequestSizeLimit(128 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 128 * 1024 * 1024, ValueCountLimit = 8192)]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    public ProductsController(IProductService products) => _products = products;

    [HttpGet("/api/store-owner/product-options")]
    public async Task<ActionResult<ApiResponse<ProductOptionsResponse>>> Options(CancellationToken cancellationToken)
        => Ok(ApiResponse<ProductOptionsResponse>.Ok(
            await _products.GetOptionsAsync(UserId(), cancellationToken)));

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ProductSummaryResponse>>> Summary(CancellationToken cancellationToken)
        => Ok(ApiResponse<ProductSummaryResponse>.Ok(
            await _products.GetSummaryAsync(UserId(), cancellationToken)));

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Create(
        [FromForm] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _products.CreateAsync(UserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { productId = product.Id },
            ApiResponse<ProductResponse>.Ok(product, "Product created successfully."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductListItemResponse>>>> List(
        [FromQuery] ProductListRequest request, CancellationToken cancellationToken)
        => Ok(ApiResponse<PagedResult<ProductListItemResponse>>.Ok(
            await _products.ListAsync(UserId(), request, cancellationToken)));

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Get(
        Guid productId, CancellationToken cancellationToken)
        => Ok(ApiResponse<ProductResponse>.Ok(
            await _products.GetAsync(UserId(), productId, cancellationToken)));

    [HttpPut("{productId:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Update(
        Guid productId, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
        => Ok(ApiResponse<ProductResponse>.Ok(
            await _products.UpdateAsync(UserId(), productId, request, cancellationToken),
            "Product updated successfully."));

    [HttpDelete("{productId:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid productId, CancellationToken cancellationToken)
    {
        await _products.DeleteAsync(UserId(), productId, cancellationToken);
        return Ok(ApiResponse.Ok("Product deleted successfully."));
    }

    [HttpGet("{productId:guid}/flash-deals")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductFlashDealResponse>>>> FlashDeals(
        Guid productId, [FromQuery] ProductFlashDealListRequest request,
        CancellationToken cancellationToken)
        => Ok(ApiResponse<PagedResult<ProductFlashDealResponse>>.Ok(
            await _products.ListFlashDealsAsync(UserId(), productId, request, cancellationToken)));

    [HttpPut("{productId:guid}/images")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> UpdateImages(
        Guid productId, [FromForm] UpdateProductImagesRequest request,
        CancellationToken cancellationToken)
        => Ok(ApiResponse<ProductResponse>.Ok(
            await _products.UpdateImagesAsync(UserId(), productId, request, cancellationToken),
            "Product images updated successfully."));

    [HttpPost("{productId:guid}/attribute-values")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> AddValues(
        Guid productId, [FromForm] AddProductValuesRequest request,
        CancellationToken cancellationToken)
        => Ok(ApiResponse<ProductResponse>.Ok(
            await _products.AddValuesAsync(UserId(), productId, request, cancellationToken),
            "Values and variant combinations added successfully."));

    // UC-70 updates a value image shared by all variants containing that value.
    [HttpPut("{productId:guid}/values/{valueId:guid}/image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> UpdateValueImage(
        Guid productId, Guid valueId, [FromForm] UpdateProductValueImageRequest request,
        CancellationToken cancellationToken)
        => Ok(ApiResponse<ProductResponse>.Ok(
            await _products.UpdateValueImageAsync(UserId(), productId, valueId, request, cancellationToken),
            "Value image updated successfully."));

    private Guid UserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(claim, out var id) ? id : throw AppException.Unauthorized();
    }
}
