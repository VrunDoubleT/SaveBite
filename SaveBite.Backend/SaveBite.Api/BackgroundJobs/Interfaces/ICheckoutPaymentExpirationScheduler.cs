namespace SaveBite.Backend.BackgroundJobs.Interfaces;

// Schedule and delete the job protecting an unpaid order's stock reservation.
public interface ICheckoutPaymentExpirationScheduler
{
    string Schedule(Guid orderId, DateTime expiresAt);
    bool Delete(string jobId);
}
