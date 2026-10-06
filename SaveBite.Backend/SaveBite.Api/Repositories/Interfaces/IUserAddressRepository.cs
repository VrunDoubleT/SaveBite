using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Repositories.Interfaces;

public interface IUserAddressRepository
{
    Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAddress> CreateAsync(UserAddress address, bool setAsDefault, CancellationToken cancellationToken = default);
    Task<UserAddress?> GetByIdAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
    Task SetDefaultAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserAddress address, bool setAsDefault, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserAddress address, CancellationToken cancellationToken = default);
}