using SaveBite.Backend.Messaging.Consumer;
using SaveBite.Backend.Messaging.Consumer.Notifications;
using SaveBite.Backend.Messaging.Consumer.Orders;
using SaveBite.Backend.Messaging.Consumer.Users;
using SaveBite.Backend.Messaging.Consumer.Email;
using SaveBite.Backend.Messaging.Consumer.Cart;
using SaveBite.Backend.Messaging.Consumer.FlashDeal;
using ProductWarningDeadlineConsumer = SaveBite.Backend.Messaging.Consumer.ProductWarnings.ProductWarningDeadlineConsumer;

namespace SaveBite.Backend.Messaging.Extensions;

public static class RabbitMqConsumerRegistration
{
    public static IServiceCollection AddAllRabbitMqConsumers(
        this IServiceCollection services)
    {
        services.AddRabbitMqConsumer<EmailConsumer>();
        services.AddRabbitMqConsumer<NotificationConsumer>();
        services.AddRabbitMqConsumer<ShopDealOpenedConsumer>();
        services.AddRabbitMqConsumer<CartFlashDealConsumer>();
        services.AddRabbitMqConsumer<RefundPlatformFeeConsumer>();
        services.AddRabbitMqConsumer<RefundTrustConsumer>();
        services.AddRabbitMqConsumer<InventoryChangedConsumer>();
        services.AddRabbitMqConsumer<OrderTrustConsumer>();
        services.AddRabbitMqConsumer<UserVerifiedConsumer>();
        services.AddRabbitMqConsumer<FlashDealConsumer>();
        services.AddRabbitMqConsumer<OrderPlatformFeeConsumer>();
        services.AddRabbitMqConsumer<ModerationConsumer>();
        services.AddHostedService<ProductWarningDeadlineConsumer>();
        return services;
    }
}