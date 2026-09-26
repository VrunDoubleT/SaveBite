using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Messaging.Models;

namespace SaveBite.Backend.Messaging.Publisher;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMqOptions _options;

    public RabbitMqPublisher(
        IConnection connection,
        IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _options = options.Value;
    }

    public async Task PublishAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default)
    {
        var rabbitMessage = new RabbitMessage<T>
        {
            EventType = routingKey,
            Data = message
        };

        var body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(rabbitMessage));

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await using var channel =
            await _connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true),
                cancellationToken);

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}