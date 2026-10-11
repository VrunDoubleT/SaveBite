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

    public async Task<IReadOnlyList<ShopApplication>> GetAllForApplicantAsync(Guid applicantUserId, CancellationToken cancellationToken = default)
        => await _dbContext.ShopApplications
            .Include(x => x.Documents)
            .Include(x => x.ReviewLogs)
            .AsSplitQuery()
            .Where(x => x.ApplicantUserId == applicantUserId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveApplicationAsync(Guid applicantUserId, CancellationToken cancellationToken = default)
        => _dbContext.ShopApplications.AnyAsync(x =>
            x.ApplicantUserId == applicantUserId &&
            (x.Status == ShopApplicationStatus.Pending
             || x.Status == ShopApplicationStatus.Approved),
            cancellationToken);

    // Manage shop applications.
    public async Task<IReadOnlyList<ShopApplication>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.ShopApplications
            .Include(x => x.Documents)
            .Include(x => x.ReviewLogs)
            .AsSplitQuery()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    // Manage shop applications.
    public Task<ShopApplication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _dbContext.ShopApplications
            .Include(x => x.ApplicantUser)
            .Include(x => x.Documents)
            .Include(x => x.ReviewLogs)
            .Include(x => x.Shop)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    
    // Manage shop applications.
    public async Task SaveApprovedApplicationAsync(ShopApplication application, CancellationToken cancellationToken = default)
    {
        var applicant = application.ApplicantUser;
        var now = DateTime.UtcNow;
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            OwnerUserId = application.ApplicantUserId,
            ApplicationId = application.Id,
            Name = application.Name,
            Description = application.Description,
            AddressLine = application.AddressLine,
            Ward = application.Ward,
            District = application.District,
            City = application.City,
            Latitude = application.Latitude,
            Longitude = application.Longitude,
            LogoUrl = application.LogoUrl,
            CoverImageUrl = application.CoverImageUrl,
            OpeningTime = application.OpeningTime,
            ClosingTime = application.ClosingTime,
            Status = ShopStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        shop.PaymentConfig = new ShopPaymentConfig
        {
            ShopId = shop.Id,
            BankName = application.BankName,
            BankAccountNumber = application.BankAccountNumber,
            BankAccountHolder = application.BankAccountHolder,
            PayosClientId = application.PayosClientId,
            PayosApiKey = application.PayosApiKey,
            PayosChecksumKey = application.PayosChecksumKey,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Shops.Add(shop);

        if (applicant.Role == UserRole.User)
        {
            applicant.Role = UserRole.StoreOwner;
            applicant.UpdatedAt = now;
            _dbContext.UserRoleChangeLogs.Add(new UserRoleChangeLog
            {
                Id = Guid.NewGuid(),
                UserId = applicant.Id,
                PreviousRole = UserRole.User,
                NewRole = UserRole.StoreOwner,
                Reason = $"Shop application {application.Id} approved by an administrator.",
                CreatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsync(ShopApplication application, CancellationToken cancellationToken = default)
        => await _dbContext.ShopApplications.AddAsync(application, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);
}
