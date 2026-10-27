using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;
namespace SaveBite.Backend.BackgroundJobs.Scheduling;

public class RefundDeadlineScheduler : IRefundDeadlineScheduler
{
    private readonly IBackgroundJobClient _jobs;
    public RefundDeadlineScheduler(IBackgroundJobClient jobs) { _jobs = jobs; }
    // Include the expected deadline so a stale job cannot expire an extended refund.
    public string Schedule(Guid refundId, DateTime deadline)
    {
        var id = _jobs.Schedule<RefundDeadlineJob>(job => job.ExecuteAsync(refundId, deadline, CancellationToken.None), new DateTimeOffset(deadline, TimeSpan.Zero));
        return !string.IsNullOrWhiteSpace(id) ? id : throw new InvalidOperationException("Refund deadline could not be scheduled.");
    }
    public bool Delete(string jobId) => _jobs.Delete(jobId);
}
