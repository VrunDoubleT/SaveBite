using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IShopApplicationRepository
{
    Task<ShopApplication?> GetByIdForApplicantAsync(Guid id, Guid applicantUserId, CancellationToken cancellationToken = default);
    Task<ShopApplication?> GetLatestForApplicantAsync(Guid applicantUserId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveApplicationAsync(Guid applicantUserId, CancellationToken cancellationToken = default);
    Task AddAsync(ShopApplication application, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
