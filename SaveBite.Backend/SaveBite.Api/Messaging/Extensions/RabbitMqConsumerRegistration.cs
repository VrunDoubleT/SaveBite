using SaveBite.Backend.Messaging.Consumer.Orders;
// RabbitMqConsumerRegistration.cs.
using SaveBite.Backend.Messaging.Consumer;
using SaveBite.Backend.Messaging.Consumer.Email;
using SaveBite.Backend.Messaging.Consumer.Cart;

namespace SaveBite.Backend.Messaging.Extensions;

public static class RabbitMqConsumerRegistration
{
    public static IServiceCollection AddAllRabbitMqConsumers(
        this IServiceCollection services)
    {
        // Example registration: services.AddRabbitMqConsumer<TestProductConsumer>();
        services.AddRabbitMqConsumer<EmailConsumer>();
        services.AddRabbitMqConsumer<CartFlashDealConsumer>();
        services.AddRabbitMqConsumer<OrderPendingConsumer>();

        return services;
    }
}
