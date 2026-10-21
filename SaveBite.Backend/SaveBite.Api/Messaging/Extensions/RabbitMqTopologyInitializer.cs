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

        // Retain NoShow events until downstream consumers are introduced.
        await channel.QueueDeclareAsync(RabbitMqQueues.OrderNoShowQueueName, durable: true,
            exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RabbitMqQueues.OrderNoShowQueueName, _options.ExchangeName,
            RabbitMqRoutingKeys.OrderNoShow, cancellationToken: cancellationToken);
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}