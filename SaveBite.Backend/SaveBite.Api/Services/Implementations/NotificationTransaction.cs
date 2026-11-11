using SaveBite.Backend.Data;

namespace SaveBite.Backend.Services.Implementations;

// Use around commands whose repositories save before staging their notification.
public static class NotificationTransaction
{
    public static async Task<T> RunAsync<T>(AppDbContext db, Func<Task<T>> command, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction != null) return await command();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var result = await command();
        await transaction.CommitAsync(ct);
        return result;
    }
    public static Task RunAsync(AppDbContext db, Func<Task> command, CancellationToken ct)
        => RunAsync(db, async () => { await command(); return true; }, ct);
}
