namespace SaveBite.Backend.BackgroundJobs.Interfaces;

// Schedule the order's no-show check at its hard pickup deadline.
public interface IShopOrderNoShowScheduler
{
    string Schedule(Guid orderId, DateTime deadline);
}
