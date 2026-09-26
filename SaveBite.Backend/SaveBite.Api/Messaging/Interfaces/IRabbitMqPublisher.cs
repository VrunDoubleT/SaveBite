namespace SaveBite.Backend.Messaging.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(
        string routingKey,
        T message,
        CancellationToken cancellationToken = default);
}