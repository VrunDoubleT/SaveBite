namespace SaveBite.Backend.BackgroundJobs.Interfaces;

public interface IFlashDealJobService
{
    Task PublishDealOpenedAsync(Guid dealId);
    Task PublishDealExpiredAsync(Guid dealId);
}