using Microsoft.EntityFrameworkCore.Storage;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Repositories.Implementations;

internal sealed class RepositoryTransaction : IRepositoryTransaction
{
    private readonly IDbContextTransaction _transaction;

    public RepositoryTransaction(IDbContextTransaction transaction)
        => _transaction = transaction;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
