using SaveBite.Backend.Authorization;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet("catalog")]
    [GuestAccess]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetActiveCategoriesAsync(cancellationToken);

        return Ok(ApiResponse<List<CategoryResponse>>.Ok(categories));
    }

    [HttpGet]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<PagedResult<CategoryDetailsResponse>>>> GetCategories(
        [FromQuery] GetCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetCategoriesAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<CategoryDetailsResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse<CategoryDetailsResponse>>> GetCategoryDetails(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<CategoryDetailsResponse>.Ok(result));
    }

    [HttpPost]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var categoryId = await _categoryService.CreateCategoryAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok($"Category created successfully with ID: {categoryId}"));
    }

    [HttpPut("{id:guid}")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse>> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await _categoryService.UpdateCategoryAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Category updated successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse>> UpdateCategoryStatus(
        Guid id,
        [FromBody] UpdateCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _categoryService.UpdateCategoryStatusAsync(id, request, cancellationToken);
        var statusMsg = request.IsActive ? "activated" : "deactivated";
        return Ok(ApiResponse.Ok($"Category {statusMsg} successfully."));
    }

    [HttpDelete("{id:guid}")]
    [AdminAccess]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _categoryService.DeleteCategoryAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Category deleted successfully."));
    }
}
