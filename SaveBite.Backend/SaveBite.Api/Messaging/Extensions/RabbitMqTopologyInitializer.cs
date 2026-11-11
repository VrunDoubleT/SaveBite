using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;

namespace SaveBite.Backend.Messaging.Extensions;

public class RabbitMqTopologyInitializer : IHostedService
{
    private readonly RabbitMqTopologyState _state;
    private readonly IConnection _connection;
    private readonly RabbitMqOptions _options;

    public RabbitMqTopologyInitializer(
        IConnection connection,
        IOptions<RabbitMqOptions> options, RabbitMqTopologyState state)
    {
        _state = state;
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

        // Keep legacy queue arguments unchanged. These three consumers park failures explicitly.
        foreach (var queue in new[] { RabbitMqQueues.EmailQueueName, RabbitMqQueues.FlashDealQueueName, RabbitMqQueues.CartFlashDealQueueName })
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RabbitMqQueues.EmailQueueName, _options.ExchangeName, RabbitMqRoutingKeys.EmailRoutingKey, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RabbitMqQueues.FlashDealQueueName, _options.ExchangeName, "flashdeal.*", cancellationToken: cancellationToken);
        foreach (var key in new[] { RabbitMqRoutingKeys.FlashDealClosed, RabbitMqRoutingKeys.FlashDealHidden, RabbitMqRoutingKeys.FlashDealExpired })
            await channel.QueueBindAsync(RabbitMqQueues.CartFlashDealQueueName, _options.ExchangeName, key, cancellationToken: cancellationToken);
        foreach (var dlq in new[] { "email_dead_letter_queue", "flashdeal_dead_letter_queue", RabbitMqQueues.CartFlashDealDeadLetterQueueName,
            RabbitMqQueues.UserTrustDeadLetterQueueName, RabbitMqQueues.OrderTrustDeadLetterQueueName, RabbitMqQueues.OrderPlatformFeeDeadLetterQueueName,
            RabbitMqQueues.NotificationDeadLetterQueueName })
            await channel.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(RabbitMqQueues.NotificationQueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "", ["x-dead-letter-routing-key"] = RabbitMqQueues.NotificationDeadLetterQueueName }, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RabbitMqQueues.NotificationQueueName, _options.ExchangeName, RabbitMqRoutingKeys.NotificationRequested, cancellationToken: cancellationToken);

        foreach (var binding in new[]
        {
            (RabbitMqQueues.ShopDealOpenedQueueName, RabbitMqRoutingKeys.ShopDealOpened),
            (RabbitMqQueues.RefundPlatformFeeQueueName, RabbitMqRoutingKeys.RefundTransferred),
            (RabbitMqQueues.RefundTrustQueueName, RabbitMqRoutingKeys.RefundTransferred),
            (RabbitMqQueues.InventoryQueueName, RabbitMqRoutingKeys.InventoryChanged)
        })
        {
            var deadLetter = binding.Item1 + "_dead_letter";
            await channel.QueueDeclareAsync(deadLetter, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            await channel.QueueDeclareAsync(binding.Item1, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "", ["x-dead-letter-routing-key"] = deadLetter },
                cancellationToken: cancellationToken);
            await channel.QueueBindAsync(binding.Item1, _options.ExchangeName, binding.Item2, cancellationToken: cancellationToken);
        }

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

        // Declare moderation topology before publishing audit events.
        await channel.QueueDeclareAsync(
            queue: RabbitMqQueues.ModerationDeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: RabbitMqQueues.ModerationQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = RabbitMqQueues.ModerationDeadLetterQueueName
            },
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: RabbitMqQueues.ModerationQueueName,
            exchange: _options.ExchangeName,
            routingKey: RabbitMqRoutingKeys.ModerationAudit,
            cancellationToken: cancellationToken);

        _state.MarkReady();
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}