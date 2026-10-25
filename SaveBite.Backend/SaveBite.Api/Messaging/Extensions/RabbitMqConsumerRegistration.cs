using SaveBite.Backend.Messaging.Consumer.Orders;
using SaveBite.Backend.Messaging.Consumer.Users;
// RabbitMqConsumerRegistration.cs.
using SaveBite.Backend.Messaging.Consumer;
using SaveBite.Backend.Messaging.Consumer.Email;
using SaveBite.Backend.Messaging.Consumer.Cart;
using SaveBite.Backend.Messaging.Consumer.FlashDeal;

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
        services.AddRabbitMqConsumer<OrderTrustConsumer>();
        services.AddRabbitMqConsumer<UserVerifiedConsumer>();

        services.AddRabbitMqConsumer<FlashDealConsumer>();
        return services;
    }
}
