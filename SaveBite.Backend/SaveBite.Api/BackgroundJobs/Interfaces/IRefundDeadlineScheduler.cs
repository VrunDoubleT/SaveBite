namespace SaveBite.Backend.BackgroundJobs.Interfaces;

// Schedule or cancel the customer's refund processing deadline.
public interface IRefundDeadlineScheduler
{
    string Schedule(Guid refundId, DateTime deadline);
    bool Delete(string jobId);
}
