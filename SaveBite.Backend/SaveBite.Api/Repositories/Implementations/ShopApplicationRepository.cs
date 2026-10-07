using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class ShopApplicationRepository : IShopApplicationRepository
{
    private readonly AppDbContext _dbContext;

    public ShopApplicationRepository(AppDbContext dbContext)
        => _dbContext = dbContext;

    public Task<ShopApplication?> GetByIdForApplicantAsync(Guid id, Guid applicantUserId, CancellationToken cancellationToken = default)
        => _dbContext.ShopApplications
            .Include(x => x.Documents)
            .Include(x => x.ReviewLogs)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == id && x.ApplicantUserId == applicantUserId, cancellationToken);

    public Task<ShopApplication?> GetLatestForApplicantAsync(Guid applicantUserId, CancellationToken cancellationToken = default)
        => _dbContext.ShopApplications
            .Include(x => x.Documents)
            .Include(x => x.ReviewLogs)
            .AsSplitQuery()
            .Where(x => x.ApplicantUserId == applicantUserId)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasActiveApplicationAsync(Guid applicantUserId, CancellationToken cancellationToken = default)
        => _dbContext.ShopApplications.AnyAsync(x =>
            x.ApplicantUserId == applicantUserId &&
            (x.Status == ShopApplicationStatus.Pending || x.Status == ShopApplicationStatus.NeedsRevision), cancellationToken);

    public async Task AddAsync(ShopApplication application, CancellationToken cancellationToken = default)
        => await _dbContext.ShopApplications.AddAsync(application, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
}
