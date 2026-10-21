using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Service;

public class FlashDealJobService  : IFlashDealJobService 
{
    private readonly IFlashDealRepository _dealRepository;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<FlashDealJobService> _logger;
    
    public FlashDealJobService(
        IFlashDealRepository dealRepository,
        IRabbitMqPublisher publisher,
        ILogger<FlashDealJobService> logger)
    {
        _dealRepository = dealRepository;
        _publisher = publisher;
        _logger = logger;
    }
    
    // Hangfire executes this method when SaleStartTime is reached
    public async Task PublishDealOpenedAsync(Guid dealId)
    {
        var deal = await _dealRepository.GetActiveDealByIdAsync(dealId);
        if (deal == null) return;
        if (deal.Status == FlashDealStatus.Pending)
        {
            _logger.LogInformation("Hangfire triggered Open for Deal: {DealId}", dealId);
            await _publisher.PublishAsync(
                RabbitMqRoutingKeys.FlashDealOpened,
                new FlashDealEvent
                {
                    FlashDealId = deal.Id
                });
        }
    }
    
    // Hangfire executes this method when OrderEndTime is reached
    public async Task PublishDealExpiredAsync(Guid dealId)
    {
        var deal = await _dealRepository.GetActiveDealByIdAsync(dealId);
        if (deal == null) return;
        // If the deal has not yet ended, publish as expired
        if (deal.Status == FlashDealStatus.OnSale || deal.Status == FlashDealStatus.Hidden || deal.Status == FlashDealStatus.Pending)
        {
            _logger.LogInformation("Hangfire triggered Expired for Deal: {DealId}", dealId);
            await _publisher.PublishAsync(
                RabbitMqRoutingKeys.FlashDealExpired,
                new FlashDealEvent
                {
                    FlashDealId = deal.Id
                });
        }
    }
    
}