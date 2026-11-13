using SaveBite.Backend.Audit;
using SaveBite.Backend.Exceptions;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class UserAddressRepository : IUserAddressRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditWriter _audit;

    public UserAddressRepository(AppDbContext dbContext, IAuditWriter audit)
    {
        _dbContext = dbContext;
        _audit = audit;
    }

    private Task<Guid[]> DefaultIdsAsync(Guid userId, CancellationToken ct) => _dbContext.UserAddresses
        .AsNoTracking().Where(x => x.UserId == userId && x.IsDefault).Select(x => x.Id).ToArrayAsync(ct);

    private void RecordDefault(Guid userId, Guid[] before, Guid[] after)
    {
        if (before.Order().SequenceEqual(after.Order())) return;
        _audit.Record("UserAddressBook", userId, "DEFAULT_ADDRESS_CHANGED",
            new { DefaultAddressIds = before }, new { DefaultAddressIds = after });
    }

    // Retrieve all addresses for the current user.
    public async Task<IReadOnlyList<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbContext.UserAddresses.AsNoTracking()
            .Where(address => address.UserId == userId)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.UpdatedAt)
            .ThenBy(address => address.CreatedAt)
            .ToListAsync(cancellationToken);

    // Retrieve default address for the current user.
    public async Task<UserAddress?> GetDefaultByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbContext.UserAddresses.AsNoTracking()
            .Where(address => address.UserId == userId)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    // Add a new address.
    public async Task<UserAddress> CreateAsync(UserAddress address, bool setAsDefault, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var previousDefaults = await DefaultIdsAsync(address.UserId, cancellationToken);
        var hasExistingAddress = await _dbContext.UserAddresses.AnyAsync(item => item.UserId == address.UserId, cancellationToken);

        // The first address is always the default address.
        var shouldBeDefault = !hasExistingAddress || setAsDefault;
        if (shouldBeDefault)
        {
            await _dbContext.UserAddresses.Where(item => item.UserId == address.UserId && item.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsDefault, false), cancellationToken);
        }

        address.IsDefault = shouldBeDefault;
        address.AddressVersion = 1;
        _dbContext.UserAddresses.Add(address);

        if (shouldBeDefault)
        {
            RecordDefault(address.UserId, previousDefaults, [address.Id]);
            UserDefaultAddressChangedOutbox.Stage(
                _dbContext,
                address.UserId,
                address.Id,
                address.Latitude,
                address.Longitude,
                address.AddressVersion,
                true);
        }

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
        var target = await _dbContext.UserAddresses.SingleOrDefaultAsync(x => x.Id == addressId && x.UserId == userId, cancellationToken);
        if (target is null)
            throw AppException.NotFound("Address not found.");

        var previousDefaults = await DefaultIdsAsync(userId, cancellationToken);
        if (previousDefaults.Length == 1 && previousDefaults[0] == addressId) return;

        await _dbContext.UserAddresses.Where(address => address.UserId == userId && address.IsDefault)
            .ExecuteUpdateAsync(setters => setters.SetProperty(address => address.IsDefault, false), cancellationToken);

        target.IsDefault = true;
        target.AddressVersion++;
        target.UpdatedAt = DateTime.UtcNow;

        UserDefaultAddressChangedOutbox.Stage(
            _dbContext,
            userId,
            addressId,
            target.Latitude,
            target.Longitude,
            target.AddressVersion,
            true);

        RecordDefault(userId, previousDefaults, [addressId]);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // Update an address.
    public async Task UpdateAsync(UserAddress address, bool setAsDefault, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var previousDefaults = await DefaultIdsAsync(address.UserId, cancellationToken);
        if (setAsDefault)
        {
            await _dbContext.UserAddresses.Where(item => item.UserId == address.UserId && item.Id != address.Id && item.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsDefault, false), cancellationToken);
        }

        var wasDefault = address.IsDefault;
        address.IsDefault = setAsDefault;
        address.AddressVersion++;
        address.UpdatedAt = DateTime.UtcNow;

        _dbContext.UserAddresses.Update(address);

        if (address.IsDefault)
        {
            UserDefaultAddressChangedOutbox.Stage(
                _dbContext,
                address.UserId,
                address.Id,
                address.Latitude,
                address.Longitude,
                address.AddressVersion,
                true);
        }
        else if (wasDefault && !setAsDefault)
        {
            UserDefaultAddressChangedOutbox.Stage(
                _dbContext,
                address.UserId,
                address.Id,
                0,
                0,
                address.AddressVersion,
                false,
                isDeleted: true);
        }

        RecordDefault(address.UserId, previousDefaults, setAsDefault ? [address.Id] : previousDefaults.Where(id => id != address.Id).ToArray());
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // Delete an address.
    public async Task DeleteAsync(UserAddress address, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        _dbContext.UserAddresses.Remove(address);

        if (address.IsDefault)
        {
            UserDefaultAddressChangedOutbox.Stage(
                _dbContext,
                address.UserId,
                address.Id,
                0,
                0,
                address.AddressVersion + 1,
                false,
                isDeleted: true);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
