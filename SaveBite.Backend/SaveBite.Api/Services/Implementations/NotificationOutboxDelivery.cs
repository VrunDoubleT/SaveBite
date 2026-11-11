using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.Services.Implementations;

// Scoped to the business DbContext. Deliver its new outbox rows only after commit.
public sealed class NotificationOutboxDelivery(IServiceScopeFactory scopes, ILogger<NotificationOutboxDelivery> logger)
{
    private readonly HashSet<Guid> pending = new();
    public void Track(Guid id) => pending.Add(id);
    public void Clear() => pending.Clear();
    public async Task FlushAsync()
    {
        var ids = pending.ToArray();
        pending.Clear();
        if (ids.Length == 0) return;
        // Independent of HTTP cancellation after commit. Bound the whole fast path.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        foreach (var id in ids)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<OrderEventDispatchJob>().ExecuteAsync(id, timeout.Token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Immediate notification publish failed for {MessageId}; committed outbox remains available for recovery.", id);
            }
        }
    }
}
