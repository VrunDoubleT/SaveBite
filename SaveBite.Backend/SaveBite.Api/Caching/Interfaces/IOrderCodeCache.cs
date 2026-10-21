namespace SaveBite.Backend.Caching.Interfaces;

// Allocate the next daily pickup code using the persisted database maximum as a floor.
public interface IOrderCodeCache
{
    Task<string> NextAsync(Guid shopId, DateOnly date, int persistedMaximum, CancellationToken ct = default);
}
