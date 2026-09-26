using SaveBite.Backend.Messaging.Consumer;
using SaveBite.Backend.Messaging.Interfaces;

namespace SaveBite.Backend.Messaging.Extensions;

public static class RabbitMqConsumerExtensions
{
    public static IServiceCollection AddRabbitMqConsumer<TConsumer>(
        this IServiceCollection services)
        where TConsumer : class, IRabbitMqConsumer
    {
        services.AddSingleton<TConsumer>();
        services.AddHostedService<RabbitMqHostedService<TConsumer>>();

        return services;
    }
}