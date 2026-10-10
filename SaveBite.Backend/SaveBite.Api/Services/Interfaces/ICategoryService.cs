using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface ICategoryService
{
    Task<PagedResult<CategoryDetailsResponse>> GetCategoriesAsync(GetCategoriesRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDetailsResponse> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Guid> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task UpdateCategoryStatusAsync(Guid categoryId, UpdateCategoryStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task<List<CategoryResponse>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
}
