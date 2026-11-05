using System.Text.Json;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IAdminAuditWriter _audit;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository repository,
        IAdminAuditWriter audit,
        TimeProvider timeProvider,
        ILogger<CategoryService> logger)
    {
        _repository = repository;
        _audit = audit;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<PagedResult<CategoryDetailsResponse>> GetCategoriesAsync(GetCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var pagedCategories = await _repository.GetPagedCategoriesAsync(request, cancellationToken);

        var items = pagedCategories.Items.Select(c => new CategoryDetailsResponse(
            c.Id,
            c.Name,
            c.Description,
            c.ImageUrl,
            c.IsActive,
            c.CreatedAt,
            c.UpdatedAt
        )).ToList();

        return PagedResult<CategoryDetailsResponse>.Create(items, pagedCategories.Page, pagedCategories.PageSize, pagedCategories.TotalItems);
    }

    public async Task<CategoryDetailsResponse> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await GetAndValidateCategoryAsync(categoryId, cancellationToken);

        return new CategoryDetailsResponse(
            category.Id,
            category.Name,
            category.Description,
            category.ImageUrl,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt
        );
    }

    public async Task<Guid> CreateCategoryAsync(Guid adminId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (await _repository.CategoryNameExistsAsync(request.Name.Trim(), null, cancellationToken))
            throw AppException.Conflict("A category with this name already exists.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        _repository.AddCategory(category);
        _audit.Record(new AuditLog
        {
            ActorUserId = adminId,
            Action = "CATEGORY_CREATED",
            TargetType = "Category",
            TargetId = category.Id,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                category.Name,
                category.Description,
                category.ImageUrl,
                category.IsActive
            }),
            CreatedAt = now
        });
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category created successfully. CategoryId: {CategoryId}", category.Id);
        return category.Id;
    }

    public async Task UpdateCategoryAsync(Guid adminId, Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await GetAndValidateCategoryAsync(categoryId, cancellationToken);

        if (await _repository.CategoryNameExistsAsync(request.Name.Trim(), categoryId, cancellationToken))
            throw AppException.Conflict("Another category with this name already exists.");

        var oldValues = new
        {
            category.Name,
            category.Description,
            category.ImageUrl,
            category.IsActive
        };
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.ImageUrl = request.ImageUrl;
        category.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        _audit.Record(new AuditLog
        {
            ActorUserId = adminId,
            Action = "CATEGORY_UPDATED",
            TargetType = "Category",
            TargetId = category.Id,
            OldValuesJson = JsonSerializer.Serialize(oldValues),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                category.Name,
                category.Description,
                category.ImageUrl,
                category.IsActive
            }),
            CreatedAt = category.UpdatedAt
        });
        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category updated successfully. CategoryId: {CategoryId}", category.Id);
    }

    public async Task UpdateCategoryStatusAsync(Guid adminId, Guid categoryId, UpdateCategoryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var category = await GetAndValidateCategoryAsync(categoryId, cancellationToken);

        if (category.IsActive == request.IsActive)
            return;

        var oldIsActive = category.IsActive;
        category.IsActive = request.IsActive;
        category.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        _audit.Record(new AuditLog
        {
            ActorUserId = adminId,
            Action = request.IsActive ? "CATEGORY_ACTIVATED" : "CATEGORY_DEACTIVATED",
            TargetType = "Category",
            TargetId = category.Id,
            OldValuesJson = JsonSerializer.Serialize(new { IsActive = oldIsActive }),
            NewValuesJson = JsonSerializer.Serialize(new { category.IsActive }),
            CreatedAt = category.UpdatedAt
        });

        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category status updated. CategoryId: {CategoryId}, IsActive: {IsActive}", category.Id, request.IsActive);
    }

    public async Task DeleteCategoryAsync(Guid adminId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await GetAndValidateCategoryAsync(categoryId, cancellationToken);

        // Check if the category is currently being used by any product.
        if (await _repository.HasLinkedProductsAsync(categoryId, cancellationToken))
        {
            throw AppException.Conflict("Cannot delete this category because it is currently linked to active products. Please update it to 'Inactive' instead.");
        }

        var oldValues = new
        {
            category.Name,
            category.Description,
            category.ImageUrl,
            category.IsActive
        };
        _repository.DeleteCategory(category);
        _audit.Record(new AuditLog
        {
            ActorUserId = adminId,
            Action = "CATEGORY_DELETED",
            TargetType = "Category",
            TargetId = category.Id,
            OldValuesJson = JsonSerializer.Serialize(oldValues),
            NewValuesJson = JsonSerializer.Serialize(new { Deleted = true }),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        });
        await _repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category deleted successfully. CategoryId: {CategoryId}", category.Id);
    }

    private async Task<Category> GetAndValidateCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _repository.GetCategoryByIdAsync(categoryId, cancellationToken);
        if (category == null) throw AppException.NotFound("Category not found.");
        return category;
    }

    public async Task<List<CategoryResponse>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetActiveCategoriesAsync(cancellationToken);

        return categories.Select(c => new CategoryResponse(
            c.Id,
            c.Name,
            c.Description,
            c.ImageUrl,
            c.IsActive
        )).ToList();
    }
}
