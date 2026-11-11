namespace SaveBite.Backend.BackgroundJobs.Scheduling;

// Deadlines are durable business data. A scheduler outage must not roll them back.
public static class SafeSchedule
{
    public static string? Try(Func<string> schedule, ILogger logger, string task, Guid id)
    {
        try
        {
            var jobId = schedule();
            if (string.IsNullOrWhiteSpace(jobId)) throw new InvalidOperationException("Hangfire returned no job ID.");
            return jobId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduling {Task} for {Id} failed; deadline recovery will handle it.", task, id);
            return null;
        }
    }
}
