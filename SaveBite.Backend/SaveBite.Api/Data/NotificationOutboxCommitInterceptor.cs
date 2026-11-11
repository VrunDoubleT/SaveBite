using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SaveBite.Backend.Data;

// Explicit business transactions dispatch only after PostgreSQL has acknowledged commit.
public sealed class NotificationOutboxCommitInterceptor : DbTransactionInterceptor
{
    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
        => eventData.Context is AppDbContext db ? db.FlushNotificationOutboxAsync() : Task.CompletedTask;

    public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext db) db.ClearNotificationOutbox();
        return Task.CompletedTask;
    }
}
