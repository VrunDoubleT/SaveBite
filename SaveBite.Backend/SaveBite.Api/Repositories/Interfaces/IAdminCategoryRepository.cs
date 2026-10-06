using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IAdminCategoryRepository
{
    Task<PagedResult<Category>> GetPagedCategoriesAsync(GetCategoriesRequest request, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> CategoryNameExistsAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default);
    Task<bool> HasLinkedProductsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    void AddCategory(Category category);
    void DeleteCategory(Category category);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}