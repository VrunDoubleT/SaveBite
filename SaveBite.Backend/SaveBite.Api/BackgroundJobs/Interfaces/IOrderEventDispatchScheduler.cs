namespace SaveBite.Backend.BackgroundJobs.Interfaces;

// Queue delivery of durable order events after their transaction commits.
public interface IOrderEventDispatchScheduler
{
    void Enqueue(Guid messageId);
}
