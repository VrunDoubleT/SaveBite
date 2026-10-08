using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/categories")]
[AllowAnonymous]
public sealed class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.Description,
                c.ImageUrl,
                c.IsActive
            ))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<CategoryResponse>>.Ok(categories));
    }
}
