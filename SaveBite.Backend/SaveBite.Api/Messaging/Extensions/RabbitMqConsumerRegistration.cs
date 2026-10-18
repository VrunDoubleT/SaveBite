// RabbitMqConsumerRegistration.cs.
using SaveBite.Backend.Messaging.Consumer;
using SaveBite.Backend.Messaging.Consumer.Email;
using SaveBite.Backend.Messaging.Consumer.FlashDeal;

namespace SaveBite.Backend.Messaging.Extensions;

public static class RabbitMqConsumerRegistration
{
    public static IServiceCollection AddAllRabbitMqConsumers(
        this IServiceCollection services)
    {
        // Example registration: services.AddRabbitMqConsumer<TestProductConsumer>();
        services.AddRabbitMqConsumer<EmailConsumer>();
        services.AddRabbitMqConsumer<FlashDealConsumer>();
        return services;
    }
}
