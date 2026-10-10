using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Messaging.Models;

namespace SaveBite.Backend.Messaging.Consumer;

public abstract class RabbitMqConsumer<T> : IRabbitMqConsumer
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    protected RabbitMqConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // Queue consumed by this consumer.
    protected abstract string QueueName { get; }

    // Topic routing key used to bind the queue to the exchange.
    // Example: order.*, deal.*, notification.*.
    protected abstract string RoutingKey { get; }

    // Business logic executed when a message is received.
    protected abstract Task HandleMessageAsync(
        RabbitMessage<T> message,
        CancellationToken cancellationToken);

    public async Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,

            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        // Create RabbitMQ connection.
        _connection = await factory.CreateConnectionAsync(
            cancellationToken);

        // Create channel.
        _channel = await _connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        // Limit the number of unacknowledged messages.
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: cancellationToken);

        // Declare Topic Exchange.
        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Declare Queue.
        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // Bind Queue to Topic Exchange.
        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: _options.ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);

        // Create consumer.
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await ProcessMessageAsync(
                eventArgs,
                cancellationToken);
        };

        // Start consuming.
        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "RabbitMQ consumer started. Queue: {QueueName}, RoutingKey: {RoutingKey}",
            QueueName,
            RoutingKey);
    }

    private async Task ProcessMessageAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        if (_channel == null)
        {
            return;
        }

        try
        {
            var message =
                JsonSerializer.Deserialize<RabbitMessage<T>>(
                    eventArgs.Body.Span);

            if (message == null)
            {
                throw new InvalidOperationException(
                    "Failed to deserialize RabbitMQ message.");
            }

            _logger.LogInformation(
                "RabbitMQ message received. " +
                "Queue: {QueueName}, RoutingKey: {RoutingKey}, MessageId: {MessageId}",
                QueueName,
                eventArgs.RoutingKey,
                message.MessageId);

            // Execute business logic.
            await HandleMessageAsync(
                message,
                cancellationToken);

            // Message processed successfully.
            await _channel.BasicAckAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "RabbitMQ message acknowledged. " +
                "Queue: {QueueName}, DeliveryTag: {DeliveryTag}",
                QueueName,
                eventArgs.DeliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process RabbitMQ message. " +
                "Queue: {QueueName}, DeliveryTag: {DeliveryTag}",
                QueueName,
                eventArgs.DeliveryTag);

            // Message will be handled by retry/DLQ strategy.
            await _channel.BasicNackAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: cancellationToken);
        }
    }

    public async Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
            _channel = null;
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
            _connection = null;
        }

        _logger.LogInformation(
            "RabbitMQ consumer stopped. Queue: {QueueName}",
            QueueName);
    }
}
