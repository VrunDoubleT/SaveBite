using SaveBite.Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Helpers;
using Microsoft.Extensions.Options;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Data;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Consumer;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Messaging.Consumer.FlashDeal;

public class FlashDealConsumer : RabbitMqConsumer<FlashDealEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected override string? DeadLetterQueueName => "flashdeal_dead_letter_queue";
    protected override bool ParkFailures => true;
    protected override int RetryAttempts => 3;

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
        if (dealEvent == null || dealEvent.FlashDealId == Guid.Empty) throw new ArgumentException("FlashDealId is required.");
        var routingKey = message.EventType;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dealRepository = scope.ServiceProvider.GetRequiredService<IFlashDealRepository>();
        var redisService = scope.ServiceProvider.GetRequiredService<IFlashDealRedisService>();
        var dealService = scope.ServiceProvider.GetRequiredService<IFlashDealService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationQueue>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deals WHERE id = {dealEvent.FlashDealId} FOR UPDATE", cancellationToken);
        var current = await db.FlashDeals.AsNoTracking().SingleOrDefaultAsync(d => d.Id == dealEvent.FlashDealId, cancellationToken);
        if (current == null) return;
        var now = DateTime.UtcNow;
        if (routingKey == RabbitMqRoutingKeys.FlashDealOpened
            && (now < current.SaleStartTime || now >= current.OrderEndTime
                || current.Status is not (FlashDealStatus.Pending or FlashDealStatus.OnSale or FlashDealStatus.SoldOut)
                || (dealEvent.ExpectedDeadline.HasValue && !RefundRules.SameDeadline(current.SaleStartTime, dealEvent.ExpectedDeadline.Value)))) return;
        if (routingKey == RabbitMqRoutingKeys.FlashDealExpired
            && (now < current.OrderEndTime || current.Status == FlashDealStatus.Cancelled
                || (dealEvent.ExpectedDeadline.HasValue && !RefundRules.SameDeadline(current.OrderEndTime, dealEvent.ExpectedDeadline.Value)))) return;
        if (routingKey == RabbitMqRoutingKeys.FlashDealHidden && current.Status != FlashDealStatus.Hidden) return;
        if (routingKey == RabbitMqRoutingKeys.FlashDealClosed && current.Status is not (FlashDealStatus.Closed or FlashDealStatus.Cancelled)) return;


        switch (routingKey)
        {
            case RabbitMqRoutingKeys.FlashDealOpened:
                await HandleDealOpenedAsync(dealEvent.FlashDealId, db, dealRepository, redisService, dealService, notificationService, cancellationToken);
                break;

            case RabbitMqRoutingKeys.FlashDealClosed:
            case RabbitMqRoutingKeys.FlashDealExpired:
                await HandleDealClosedOrExpiredAsync(dealEvent.FlashDealId, dealRepository, redisService, cancellationToken);
                break;

            case RabbitMqRoutingKeys.FlashDealHidden:
                await HandleDealHiddenAsync(dealEvent.FlashDealId, dealRepository, redisService, cancellationToken);
                break;
        }
        await db.FlashDeals.Where(d => d.Id == dealEvent.FlashDealId)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.LifecycleSyncedAt, (DateTime?)DateTime.UtcNow), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task HandleDealOpenedAsync(
        Guid flashDealId,
        AppDbContext db,
        IFlashDealRepository dealRepository,
        IFlashDealRedisService redisService,
        IFlashDealService dealService,
        INotificationQueue notificationService,
        CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetActiveDealByIdAsync(flashDealId, cancellationToken);
        if (deal == null) return;

        if (deal.Status == FlashDealStatus.Pending && (deal.Shop.Status != ShopStatus.Active
            || deal.Product.Status != ProductStatus.Active || await WarningDealPolicy.IsBlockedAsync(db, deal.ProductId, cancellationToken)))
            return;
        // Update DB to 'OnSale'  if currently 'Pending'
        if (deal.Status == FlashDealStatus.Pending)
        {
            deal.Status = FlashDealStatus.OnSale;
            await dealRepository.UpdateStatusAsync(deal.Id, FlashDealStatus.OnSale, cancellationToken);
        }

        var inventoryStatus = deal.Variants.Any(v => v.Status == FlashDealVariantStatus.Active && v.Variant.IsActive
            && (long)v.TotalQuantity - v.ReservedQuantity - v.SoldQuantity > 0) ? FlashDealStatus.OnSale : FlashDealStatus.SoldOut;
        if (deal.Status != inventoryStatus)
        {
            deal.Status = inventoryStatus;
            await dealRepository.UpdateStatusAsync(deal.Id, inventoryStatus, cancellationToken);
        }

        // Push deal information into Redis Cache for customer searches
        var customerResponse = FlashDealService.MapToResponse(deal);
        if (deal.Shop != null)
        {
            await redisService.SaveDealAsync(customerResponse, deal.Shop.Latitude, deal.Shop.Longitude);

            if (deal.Status == FlashDealStatus.OnSale) await notificationService.EnqueueAsync(
                    NotificationTarget.Customers(),
                    new CreateNotificationCommand(
                        NotificationType.FlashDealAvailable,
                        $"New Flash Deal from {deal.Shop.Name}!",
                        $"'{customerResponse.ProductName}' is now on sale. Order now before it runs out!",
                        ActionUrl: NotificationActionUrls.FlashDeal(deal.Id),
                        DeduplicationKey: $"flash-deal-opened:{deal.Id}"),
                    cancellationToken);
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
