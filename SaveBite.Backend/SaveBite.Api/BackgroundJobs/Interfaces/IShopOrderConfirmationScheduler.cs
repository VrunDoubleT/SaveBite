namespace SaveBite.Backend.BackgroundJobs.Interfaces;

// Schedule or remove the shop's five-minute confirmation timeout.
public interface IShopOrderConfirmationScheduler
{
    string Schedule(Guid orderId, DateTime deadline);
    bool Delete(string jobId);
}
