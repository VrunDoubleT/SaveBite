namespace SaveBite.Backend.Repositories.Interfaces;

public interface IRepositoryTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
