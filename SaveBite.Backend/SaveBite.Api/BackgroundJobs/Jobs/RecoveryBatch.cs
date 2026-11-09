using SaveBite.Backend.Configurations;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Keyset pages and a fresh scope per item prevent skipped rows and poisoned tracking after errors.
public static class RecoveryBatch
{
    public static async Task RunAsync(IServiceScopeFactory scopes, ILogger logger, string group,
        Func<AppDbContext, IQueryable<Guid>> candidates, Func<IServiceProvider, Guid, Task> execute, CancellationToken ct)
    {
        Guid? cursor = null;
        while (true)
        {
            List<Guid> ids;
            await using (var queryScope = scopes.CreateAsyncScope())
            {
                var query = candidates(queryScope.ServiceProvider.GetRequiredService<AppDbContext>());
                if (cursor.HasValue) query = query.Where(id => id.CompareTo(cursor.Value) > 0);
                ids = await query.OrderBy(id => id).Take(TimingConfiguration.Jobs.RecoveryBatchSize).ToListAsync(ct);
            }
            if (ids.Count == 0) return;
            foreach (var id in ids)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    await execute(scope.ServiceProvider, id);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                { logger.LogError(ex, "Recovery {Group} failed for {Id}; continuing.", group, id); }
            }
            cursor = ids[^1];
        }
    }
}
