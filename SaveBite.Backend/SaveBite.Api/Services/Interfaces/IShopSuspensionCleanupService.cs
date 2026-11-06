namespace SaveBite.Backend.Services.Interfaces;

public interface IShopSuspensionCleanupService
{
    Task CleanupAsync(Guid shopId, CancellationToken cancellationToken = default);
}
