using System.Data;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class UserAddressRepository : IUserAddressRepository
{
    private readonly AppDbContext _dbContext;

    public UserAddressRepository(AppDbContext dbContext) => _dbContext = dbContext;

    // Retrieve all addresses for the current user.
    public async Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbContext.UserAddresses.AsNoTracking()
            .Where(address => address.UserId == userId)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.UpdatedAt)
            .ThenBy(address => address.CreatedAt)
            .ToListAsync(cancellationToken);

    // Add a new address.
    public async Task<UserAddress> CreateAsync(UserAddress address, bool setAsDefault, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var hasExistingAddress = await _dbContext.UserAddresses.AnyAsync(item => item.UserId == address.UserId, cancellationToken);

        // The first address is always the default address.
        var shouldBeDefault = !hasExistingAddress || setAsDefault;
        if (shouldBeDefault)
            await _dbContext.UserAddresses.Where(item => item.UserId == address.UserId && item.IsDefault).ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsDefault, false), cancellationToken);

        address.IsDefault = shouldBeDefault;
        _dbContext.UserAddresses.Add(address);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return address;
    }

    // Retrieve an address by ID.
    public Task<UserAddress?> GetByIdAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
        => _dbContext.UserAddresses.SingleOrDefaultAsync(address => address.Id == addressId && address.UserId == userId, cancellationToken);

    // Set the address as the default.
    public async Task SetDefaultAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await _dbContext.UserAddresses.Where(address => address.UserId == userId && address.IsDefault).ExecuteUpdateAsync(setters => setters.SetProperty(address => address.IsDefault, false), cancellationToken);
        await _dbContext.UserAddresses.Where(address => address.UserId == userId && address.Id == addressId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(address => address.IsDefault, true).SetProperty(address => address.UpdatedAt, DateTime.UtcNow), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // Update an address.
    public async Task UpdateAsync(UserAddress address, bool setAsDefault, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (setAsDefault)
        {
            await _dbContext.UserAddresses.Where(item => item.UserId == address.UserId && item.Id != address.Id && item.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsDefault, false), cancellationToken);
        }

        address.IsDefault = setAsDefault;
        address.UpdatedAt = DateTime.UtcNow;

        _dbContext.UserAddresses.Update(address);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // Delete an address.
    public async Task DeleteAsync( UserAddress address, CancellationToken cancellationToken = default)
    {
        _dbContext.UserAddresses.Remove(address);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
