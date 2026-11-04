using Npgsql;
using System.Text.Json;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using System.Data;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _dbContext;

    public AuthRepository(AppDbContext dbContext)
        => _dbContext = dbContext;

    public Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default)
        => _dbContext.Users.AnyAsync(
            user => user.Email == email,
            cancellationToken);

    public Task<User?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
        => _dbContext.Users.SingleOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);

    public Task<User?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _dbContext.Users
            .Include(user => user.Addresses)
            .SingleOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);

    public async Task<bool> TryAddUserAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(user);
        OrderEventOutbox? verifiedEvent = null;
        if (user.EmailVerifiedAt.HasValue)
        {
            verifiedEvent = new OrderEventOutbox { Id = Guid.NewGuid(), EventKey = $"user:{user.Id}:verified",
                RoutingKey = RabbitMqRoutingKeys.UserVerified,
                PayloadJson = JsonSerializer.Serialize(new UserVerifiedEvent(user.Id, user.EmailVerifiedAt.Value)),
                CreatedAt = user.CreatedAt };
            _dbContext.Add(verifiedEvent);
        }
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _dbContext.Entry(user).State = EntityState.Detached;
            if (verifiedEvent != null) _dbContext.Entry(verifiedEvent).State = EntityState.Detached;
            return false;
        }
    }

    public Task<RefreshToken?> GetRefreshTokenWithUserAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
        => _dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);

    public void AddRefreshToken(RefreshToken refreshToken)
        => _dbContext.RefreshTokens.Add(refreshToken);

    public async Task RevokeAllActiveRefreshTokensAsync(
        Guid userId,
        string reason,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
        // Execute a single set-based update instead of loading every token entity.
        => await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, revokedAt)
                    .SetProperty(token => token.RevokeReason, reason),
                cancellationToken);

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
        => await _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IRepositoryTransaction> BeginSerializableTransactionAsync(
        CancellationToken cancellationToken = default)
        => new RepositoryTransaction(
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken));
}
