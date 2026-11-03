using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
namespace SaveBite.Backend.BackgroundJobs.Service;

public sealed class FlashDealJobService(IFlashDealRepository deals, IRabbitMqPublisher publisher, TimeProvider clock) : IFlashDealJobService
{
    // Keep these entry points for jobs persisted before deadline-aware scheduling.
    public Task PublishDealOpenedAsync(Guid id) => PublishAsync(id, null, true);
    public Task PublishDealExpiredAsync(Guid id) => PublishAsync(id, null, false);
    public Task PublishDealOpenedAtAsync(Guid id, DateTime expectedDeadline) => PublishAsync(id, expectedDeadline, true);
    public Task PublishDealExpiredAtAsync(Guid id, DateTime expectedDeadline) => PublishAsync(id, expectedDeadline, false);

    private async Task PublishAsync(Guid id, DateTime? expected, bool opening)
    {
        var deal = await deals.GetActiveDealByIdAsync(id);
        if (deal == null) return;
        var now = clock.GetUtcNow().UtcDateTime;
        var deadline = opening ? deal.SaleStartTime : deal.OrderEndTime;
        if (expected.HasValue && !RefundRules.SameDeadline(deadline, expected.Value)) return;
        if (opening)
        {
            if (now < deal.SaleStartTime || now >= deal.OrderEndTime
                || deal.Status is not (FlashDealStatus.Pending or FlashDealStatus.OnSale or FlashDealStatus.SoldOut)) return;
        }
        else if (now < deal.OrderEndTime || deal.Status == FlashDealStatus.Cancelled) return;
        await publisher.PublishAsync(opening ? RabbitMqRoutingKeys.FlashDealOpened : RabbitMqRoutingKeys.FlashDealExpired,
            new FlashDealEvent { FlashDealId = id, ExpectedDeadline = deadline });
    }
}
