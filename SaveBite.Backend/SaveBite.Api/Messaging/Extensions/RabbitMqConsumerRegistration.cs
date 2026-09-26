// RabbitMqConsumerRegistration.cs
using SaveBite.Backend.Messaging.Consumer;
using SaveBite.Backend.Messaging.Consumer.Email;

namespace SaveBite.Backend.Messaging.Extensions;

public static class RabbitMqConsumerRegistration
{
    public static IServiceCollection AddAllRabbitMqConsumers(
        this IServiceCollection services)
    {
        // services.AddRabbitMqConsumer<TestProductConsumer>();
        services.AddRabbitMqConsumer<EmailConsumer>();

        return services;
    }
}