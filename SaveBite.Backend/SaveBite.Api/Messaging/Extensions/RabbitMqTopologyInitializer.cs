using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;

namespace SaveBite.Backend.Messaging.Extensions;

public class RabbitMqTopologyInitializer : IHostedService
{
    private readonly IConnection _connection;
    private readonly RabbitMqOptions _options;

    public RabbitMqTopologyInitializer(
        IConnection connection,
        IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _options = options.Value;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await using var channel =
            await _connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Retain verified-account events even while the trust consumer is offline.
        await channel.QueueDeclareAsync(RabbitMqQueues.UserTrustQueueName, durable: true,
            exclusive: false, autoDelete: false, arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = RabbitMqQueues.UserTrustDeadLetterQueueName
            }, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RabbitMqQueues.UserTrustQueueName, _options.ExchangeName,
            RabbitMqRoutingKeys.UserVerified, cancellationToken: cancellationToken);

        // Bind trust events before outbox publishing, including when its consumer has not started yet.
        await channel.QueueDeclareAsync(RabbitMqQueues.OrderTrustQueueName, durable: true,
            exclusive: false, autoDelete: false, arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = RabbitMqQueues.OrderTrustDeadLetterQueueName
            }, cancellationToken: cancellationToken);
        foreach (var key in new[] { RabbitMqRoutingKeys.OrderCompleted, RabbitMqRoutingKeys.OrderNoShow })
            await channel.QueueBindAsync(RabbitMqQueues.OrderTrustQueueName, _options.ExchangeName, key, cancellationToken: cancellationToken);

        // Retain NoShow events until downstream consumers are introduced.
        await channel.QueueDeclareAsync(RabbitMqQueues.OrderNoShowQueueName, durable: true,
            exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RabbitMqQueues.OrderNoShowQueueName, _options.ExchangeName,
            RabbitMqRoutingKeys.OrderNoShow, cancellationToken: cancellationToken);
        // Bind platform-fee accrual on completed orders.
        await channel.QueueDeclareAsync(RabbitMqQueues.OrderPlatformFeeQueueName, durable: true,
            exclusive: false, autoDelete: false, arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = RabbitMqQueues.OrderPlatformFeeDeadLetterQueueName
            }, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RabbitMqQueues.OrderPlatformFeeQueueName, _options.ExchangeName,
            RabbitMqRoutingKeys.OrderCompleted, cancellationToken: cancellationToken);
    }
    
    

    public Task StopAsync(
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}