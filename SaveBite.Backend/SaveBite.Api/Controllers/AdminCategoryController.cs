using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = "Admin")]
public class AdminCategoryController : ControllerBase
{
    private readonly IAdminCategoryService _adminCategoryService;

    public AdminCategoryController(IAdminCategoryService adminCategoryService)
    {
        _adminCategoryService = adminCategoryService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CategoryResponse>>>> GetCategories(
        [FromQuery] GetCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.GetCategoriesAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<CategoryResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetCategoryDetails(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _adminCategoryService.GetCategoryByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<CategoryResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse>> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var categoryId = await _adminCategoryService.CreateCategoryAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok($"Category created successfully with ID: {categoryId}"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await _adminCategoryService.UpdateCategoryAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Category updated successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateCategoryStatus(
        Guid id,
        [FromBody] UpdateCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _adminCategoryService.UpdateCategoryStatusAsync(id, request, cancellationToken);
        var statusMsg = request.IsActive ? "activated" : "deactivated";
        return Ok(ApiResponse.Ok($"Category {statusMsg} successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _adminCategoryService.DeleteCategoryAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Category deleted successfully."));
    }
}