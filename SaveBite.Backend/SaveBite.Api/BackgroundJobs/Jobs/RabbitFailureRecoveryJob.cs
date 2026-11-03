using Hangfire;
using RabbitMQ.Client;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Recover failed subscribers independently; PublishedAt only acknowledges broker delivery.
public sealed class RabbitFailureRecoveryJob(IConnection connection, RabbitMqTopologyState topology,
    TimeProvider clock, ILogger<RabbitFailureRecoveryJob> logger)
{
    public static readonly IReadOnlyList<(string Source, string Destination)> Queues = new[]
    {
        ("email_dead_letter_queue", RabbitMqQueues.EmailQueueName),
        ("flashdeal_dead_letter_queue", RabbitMqQueues.FlashDealQueueName),
        (RabbitMqQueues.CartFlashDealDeadLetterQueueName, RabbitMqQueues.CartFlashDealQueueName),
        (RabbitMqQueues.NotificationDeadLetterQueueName, RabbitMqQueues.NotificationQueueName),
        (RabbitMqQueues.UserTrustDeadLetterQueueName, RabbitMqQueues.UserTrustQueueName),
        (RabbitMqQueues.OrderTrustDeadLetterQueueName, RabbitMqQueues.OrderTrustQueueName),
        (RabbitMqQueues.OrderPlatformFeeDeadLetterQueueName, RabbitMqQueues.OrderPlatformFeeQueueName),
        (RabbitMqQueues.RefundPlatformFeeQueueName + "_dead_letter", RabbitMqQueues.RefundPlatformFeeQueueName),
        (RabbitMqQueues.RefundTrustQueueName + "_dead_letter", RabbitMqQueues.RefundTrustQueueName),
        (RabbitMqQueues.InventoryQueueName + "_dead_letter", RabbitMqQueues.InventoryQueueName)
    };

    public static TimeSpan Delay(int attempt) => TimeSpan.FromMinutes(Math.Min(60, 5 * (1 << Math.Clamp(attempt, 0, 4))));

    [DisableConcurrentExecution(600)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        await topology.Ready.WaitAsync(ct);
        foreach (var (source, destination) in Queues)
        {
            await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(
                publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
            var count = await channel.MessageCountAsync(source, ct);
            // Process only the original bounded batch; never loop over messages rotated below.
            for (var index = 0; index < Math.Min(count, 100u); index++)
            {
                var item = await channel.BasicGetAsync(source, autoAck: false, ct);
                if (item == null) break;
                var properties = new BasicProperties(item.BasicProperties);
                var headers = properties.Headers = properties.Headers == null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(properties.Headers);
                var attempt = Number(headers, "recovery-attempt");
                var now = clock.GetUtcNow().ToUnixTimeSeconds();
                var due = Number(headers, "recovery-not-before") <= now;
                if (due)
                {
                    headers["recovery-attempt"] = attempt + 1;
                    headers["recovery-not-before"] = now + (long)Delay((int)Math.Min(attempt, 4)).TotalSeconds;
                    logger.LogWarning("Replaying failed message from {Source} to {Destination}, attempt {Attempt}.", source, destination, attempt + 1);
                }
                // Preserve envelope identity, headers and persistence. ACK only after confirmed publication.
                properties.Persistent = true;
                await channel.BasicPublishAsync("", due ? destination : source, mandatory: true,
                    basicProperties: properties, body: item.Body, cancellationToken: ct);
                await channel.BasicAckAsync(item.DeliveryTag, false, ct);
            }
        }
    }

    private static long Number(IDictionary<string, object?> headers, string key)
        => headers.TryGetValue(key, out var value) && value is long l ? l
            : value is int i ? i : 0;
}
