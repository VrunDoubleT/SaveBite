using SaveBite.Backend.Configurations;
using Hangfire;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class OrderOutboxRecoveryJob(IServiceScopeFactory scopes, ILogger<OrderOutboxRecoveryJob> logger,
    SaveBite.Backend.Messaging.Configuration.RabbitMqTopologyState topology)
{
    [DisableConcurrentExecution(TimingConfiguration.Jobs.ConcurrencyLockTimeoutSeconds)]
    public Task ExecuteAsync(CancellationToken ct)
        => RecoveryBatch.RunAsync(scopes, logger, "order-outbox",
            db => db.Set<OrderEventOutbox>().Where(m => m.PublishedAt == null
                && (topology.Ready.IsCompletedSuccessfully || m.RoutingKey.StartsWith("internal."))).Select(m => m.Id),
            (sp, id) => sp.GetRequiredService<OrderEventDispatchJob>().ExecuteAsync(id, ct), ct);
}
