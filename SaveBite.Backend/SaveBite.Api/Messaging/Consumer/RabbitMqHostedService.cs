using SaveBite.Backend.Messaging.Interfaces;

namespace SaveBite.Backend.Messaging.Consumer;

public class RabbitMqHostedService<TConsumer> : BackgroundService
    where TConsumer : IRabbitMqConsumer
{
    private readonly TConsumer _consumer;
    private readonly ILogger<RabbitMqHostedService<TConsumer>> _logger;

    public RabbitMqHostedService(TConsumer consumer, ILogger<RabbitMqHostedService<TConsumer>> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _consumer.StartAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
        finally
        {
            await _consumer.StopAsync();
        }
    }
}