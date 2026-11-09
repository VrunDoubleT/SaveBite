using SaveBite.Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Helpers;
using Microsoft.Extensions.Options;
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
    private readonly ILogger<FlashDealConsumer> _logger;

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
        _logger = logger;
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


        Guid? openingMessageId = null;
        switch (routingKey)
        {
            case RabbitMqRoutingKeys.FlashDealOpened:
                openingMessageId = await HandleDealOpenedAsync(dealEvent.FlashDealId, db, dealRepository, cancellationToken);
                break;

            case RabbitMqRoutingKeys.FlashDealClosed:
            case RabbitMqRoutingKeys.FlashDealExpired:
                await HandleDealClosedOrExpiredAsync(dealEvent.FlashDealId, db, dealRepository, cancellationToken);
                break;

            case RabbitMqRoutingKeys.FlashDealHidden:
                // The command already persisted Hidden; DB reads exclude it immediately.
                break;
        }
        await db.FlashDeals.Where(d => d.Id == dealEvent.FlashDealId)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.LifecycleSyncedAt, (DateTime?)DateTime.UtcNow), cancellationToken);
        var action = current.Status switch
        {
            FlashDealStatus.Hidden => "hidden",
            FlashDealStatus.Closed => "closed",
            FlashDealStatus.Cancelled => "cancelled",
            FlashDealStatus.OnSale => "opened",
            FlashDealStatus.Pending => "opened",
            _ => "updated"
        };
        FlashDealRealtimeOutbox.Stage(db, current.Id, current.ShopId, action, current.Status.ToString(), current.VisibilityVersion);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (openingMessageId.HasValue)
        {
            // Best-effort fast path after commit. A fresh scope avoids reusing the lifecycle transaction.
            // The same dispatcher/row lock is used by Hangfire recovery.
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                await using var deliveryScope = _scopeFactory.CreateAsyncScope();
                await deliveryScope.ServiceProvider.GetRequiredService<SaveBite.Backend.BackgroundJobs.Jobs.OrderEventDispatchJob>()
                    .ExecuteAsync(openingMessageId.Value, timeout.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Immediate deal notification publish failed for {MessageId}; outbox recovery will retry.", openingMessageId.Value);
            }
        }
    }

    private async Task<Guid?> HandleDealOpenedAsync(
        Guid flashDealId,
        AppDbContext db,
        IFlashDealRepository dealRepository,
        CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetActiveDealByIdAsync(flashDealId, cancellationToken);
        if (deal == null) return null;

        if (deal.Status == FlashDealStatus.Pending && (deal.Shop.Status != ShopStatus.Active
            || deal.Product.Status != ProductStatus.Active || await WarningDealPolicy.IsBlockedAsync(db, deal.ProductId, cancellationToken)))
            return null;
        // Update DB to 'OnSale'  if currently 'Pending'
        if (deal.Status == FlashDealStatus.Pending)
        {
            var previousStatus = deal.Status;
            deal.Status = FlashDealStatus.OnSale;
            db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
            {
                FlashDealId = deal.Id,
                FromStatus = previousStatus,
                ToStatus = FlashDealStatus.OnSale,
                ChangedByType = "System",
                Note = "Deal sale start time was reached.",
                CreatedAt = DateTime.UtcNow
            });
            await dealRepository.UpdateStatusAsync(deal.Id, FlashDealStatus.OnSale, cancellationToken);
        }

        var inventoryStatus = deal.Variants.Any(v => v.Status == FlashDealVariantStatus.Active && v.Variant.IsActive
            && (long)v.TotalQuantity - v.ReservedQuantity - v.SoldQuantity > 0) ? FlashDealStatus.OnSale : FlashDealStatus.SoldOut;
        if (deal.Status != inventoryStatus)
        {
            var previousStatus = deal.Status;
            deal.Status = inventoryStatus;
            db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
            {
                FlashDealId = deal.Id,
                FromStatus = previousStatus,
                ToStatus = inventoryStatus,
                ChangedByType = "System",
                Note = inventoryStatus == FlashDealStatus.SoldOut
                    ? "Deal marked sold out because no deal inventory is available."
                    : "Deal inventory became available.",
                CreatedAt = DateTime.UtcNow
            });
            await dealRepository.UpdateStatusAsync(deal.Id, inventoryStatus, cancellationToken);
        }

        // Stage the opening notification in the same DB transaction.
        var customerResponse = FlashDealService.MapToResponse(deal);
        if (customerResponse.IsPurchasable)
            return await ShopDealOpenedOutbox.StageAsync(db, deal.Id, deal.ShopId, DateTime.UtcNow, cancellationToken);
        return null;
    }

    private async Task HandleDealClosedOrExpiredAsync(
        Guid flashDealId,
        AppDbContext db,
        IFlashDealRepository dealRepository,
        CancellationToken cancellationToken)
    {
        var deal = await dealRepository.GetActiveDealByIdAsync(flashDealId, cancellationToken);
        if (deal == null) return;

        if (deal.Status != FlashDealStatus.Closed && deal.Status != FlashDealStatus.Cancelled)
        {
            var previousStatus = deal.Status;
            deal.Status = FlashDealStatus.Closed;
            db.FlashDealStatusHistories.Add(new FlashDealStatusHistory
            {
                FlashDealId = deal.Id,
                FromStatus = previousStatus,
                ToStatus = FlashDealStatus.Closed,
                ChangedByType = "System",
                Note = "Deal order period ended.",
                CreatedAt = DateTime.UtcNow
            });
            await dealRepository.UpdateStatusAsync(deal.Id, FlashDealStatus.Closed, cancellationToken);
        }
    }

}
