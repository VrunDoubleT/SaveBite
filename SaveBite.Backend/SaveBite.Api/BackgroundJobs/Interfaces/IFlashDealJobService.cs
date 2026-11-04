namespace SaveBite.Backend.BackgroundJobs.Interfaces;

public interface IFlashDealJobService
{
    Task PublishDealOpenedAsync(Guid dealId);
    Task PublishDealExpiredAsync(Guid dealId);
    Task PublishDealOpenedAtAsync(Guid dealId, DateTime expectedDeadline);
    Task PublishDealExpiredAtAsync(Guid dealId, DateTime expectedDeadline);
}