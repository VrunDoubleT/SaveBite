using SaveBite.Backend.Models.Common;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<Category>> GetPagedCategoriesAsync(GetCategoriesRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchTerm));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == request.IsActive.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Category>.Create(items, request.Page, request.PageSize, totalItems);
    }

    public Task<Category?> GetCategoryByIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
    }

    public Task<bool> CategoryNameExistsAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories.AsNoTracking().Where(c => c.Name.ToLower() == name.ToLower());

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCategoryId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasLinkedProductsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        // Check if there are any products linked to this category.
        return _dbContext.Products.AsNoTracking().AnyAsync(p => p.CategoryId == categoryId, cancellationToken);
    }

    public void AddCategory(Category category)
    {
        _dbContext.Categories.Add(category);
    }

    public void DeleteCategory(Category category)
    {
        _dbContext.Categories.Remove(category);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Serialize category writes across API instances so concurrent hides/deletes
        // cannot both remove the last remaining active categories.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlRawAsync(
            "LOCK TABLE categories IN SHARE ROW EXCLUSIVE MODE", cancellationToken);
        _dbContext.ChangeTracker.DetectChanges();
        var changes = _dbContext.ChangeTracker.Entries<Category>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        var changedIds = changes.Select(e => e.Entity.Id).ToList();
        var hasActiveChange = changes.Any(e => e.State != EntityState.Deleted && e.Entity.IsActive);
        var hasOtherActive = await _dbContext.Categories.AsNoTracking()
            .AnyAsync(c => c.IsActive && !changedIds.Contains(c.Id), cancellationToken);
        if (!hasActiveChange && !hasOtherActive)
            throw AppException.Conflict("At least one category must remain active. Activate another category first.");

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<List<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
