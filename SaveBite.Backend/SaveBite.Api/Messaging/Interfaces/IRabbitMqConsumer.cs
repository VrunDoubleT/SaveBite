namespace SaveBite.Backend.Messaging.Interfaces;

public interface IRabbitMqConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}