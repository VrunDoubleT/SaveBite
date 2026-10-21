using Hangfire;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;

namespace SaveBite.Backend.BackgroundJobs.Scheduling;

// Enqueue Hangfire delivery jobs for durable outbox events.
public sealed class OrderEventDispatchScheduler : IOrderEventDispatchScheduler
{
    private readonly IBackgroundJobClient _jobs;
    public OrderEventDispatchScheduler(IBackgroundJobClient jobs) { _jobs = jobs; }

    // Queue an outbox dispatch job after the event has been saved.
    public void Enqueue(Guid messageId)
    {
        if (string.IsNullOrWhiteSpace(_jobs.Enqueue<OrderEventDispatchJob>(job => job.ExecuteAsync(messageId, CancellationToken.None))))
            throw new InvalidOperationException("Order event dispatch could not be scheduled.");
    }
}
