using Microsoft.Extensions.Options;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Consumer;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Messaging.Consumer.FlashDeal;

public class FlashDealConsumer : RabbitMqConsumer<FlashDealEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected override string QueueName => RabbitMqQueues.FlashDealQueueName;

    // Listen for all events with the prefix "flashdeal."
    protected override string RoutingKey => "flashdeal.*";

    public FlashDealConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<FlashDealConsumer> logger,
        IServiceScopeFactory scopeFactory)
        : base(options, logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleMessageAsync(
        RabbitMessage<FlashDealEvent> message,
        CancellationToken cancellationToken)
    {
        var dealEvent = message.Data;
        var routingKey = message.EventType; 

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dealRepository = scope.ServiceProvider.GetRequiredService<IFlashDealRepository>();
        var redisService = scope.ServiceProvider.GetRequiredService<IFlashDealRedisService>();
        var dealService = scope.ServiceProvider.GetRequiredService<IFlashDealService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationQueue>();

        switch (routingKey)
        {
            case RabbitMqRoutingKeys.FlashDealOpened:
                await HandleDealOpenedAsync(dealEvent.FlashDealId, dealRepository, redisService, dealService, notificationService, cancellationToken);
                break;

            case RabbitMqRoutingKeys.FlashDealClosed:
            case RabbitMqRoutingKeys.FlashDealExpired:
                await HandleDealClosedOrExpiredAsync(dealEvent.FlashDealId, dealRepository, redisService, cancellationToken);
                break;

            case RabbitMqRoutingKeys.FlashDealHidden:
                await HandleDealHiddenAsync(dealEvent.FlashDealId, dealRepository, redisService, cancellationToken);
                break;
        }
    }

    private async Task HandleDealOpenedAsync(
        Guid flashDealId,
        IFlashDealRepository dealRepository,
        IFlashDealRedisService redisService,
        IFlashDealService dealService,
        INotificationQueue notificationService,
        CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetActiveDealByIdAsync(flashDealId, cancellationToken);
        if (deal == null) return;

        // Update DB to 'OnSale' if currently 'Pending'
        if (deal.Status == FlashDealStatus.Pending)
        {
            deal.Status = FlashDealStatus.OnSale;
            await dealRepository.UpdateStatusAsync(deal.Id, FlashDealStatus.OnSale, cancellationToken);
        }

        // Push deal information into Redis Cache for customer searches
        var customerResponse = await dealService.GetByIdAsync(deal.Id, cancellationToken);
        if (deal.Shop != null)
        {
            await redisService.SaveDealAsync(customerResponse, deal.Shop.Latitude, deal.Shop.Longitude);

            try
            {
                await notificationService.EnqueueAsync(
                    NotificationTarget.Customers(),
                    new CreateNotificationCommand(
                        NotificationType.FlashDealAvailable,
                        $"New Flash Deal from {deal.Shop.Name}!",
                        $"'{customerResponse.ProductName}' is now on sale. Order now before it runs out!",
                        ActionUrl: NotificationActionUrls.FlashDeal(deal.Id),
                        DeduplicationKey: $"flash-deal-opened:{deal.Id}"),
                    cancellationToken);
            }
            catch
            {
                // Do not fail deal opening if notification dispatch encounters an issue.
            }
        }
    }

    private async Task HandleDealClosedOrExpiredAsync(
        Guid flashDealId,
        IFlashDealRepository dealRepository,
        IFlashDealRedisService redisService,
        CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetActiveDealByIdAsync(flashDealId, cancellationToken);
        if (deal == null) return;

        if (deal.Status != FlashDealStatus.Closed && deal.Status != FlashDealStatus.Cancelled)
        {
            deal.Status = FlashDealStatus.Closed;
            await dealRepository.UpdateStatusAsync(deal.Id, FlashDealStatus.Closed, cancellationToken);
        }

        // Remove from Redis Cache
        await redisService.DeleteDealAsync(deal.Id, deal.ShopId);
    }

    private async Task HandleDealHiddenAsync(
        Guid flashDealId,
        IFlashDealRepository dealRepository,
        IFlashDealRedisService redisService,
        CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetActiveDealByIdAsync(flashDealId, cancellationToken);
        if (deal == null) return;

        // When a deal is temporarily hidden: Remove from Redis Cache
        await redisService.DeleteDealAsync(deal.Id, deal.ShopId);
    }
}
