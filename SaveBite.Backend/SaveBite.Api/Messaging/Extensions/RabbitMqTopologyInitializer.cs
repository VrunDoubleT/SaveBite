using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SaveBite.Backend.Messaging.Configuration;

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
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}