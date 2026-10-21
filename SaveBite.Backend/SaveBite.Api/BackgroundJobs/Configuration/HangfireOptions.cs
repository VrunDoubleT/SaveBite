using System.ComponentModel.DataAnnotations;

namespace SaveBite.Backend.BackgroundJobs.Configuration;

public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    [Range(1, 100)]
    public int WorkerCount { get; set; } = 5;

    [Range(0, 10)]
    public int RetryAttempts { get; set; } = 3;

    public bool DashboardEnabled { get; set; }
}
