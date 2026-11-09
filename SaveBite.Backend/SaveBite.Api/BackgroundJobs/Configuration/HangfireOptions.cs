using SaveBite.Backend.Configurations;
using System.ComponentModel.DataAnnotations;

namespace SaveBite.Backend.BackgroundJobs.Configuration;

public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    [Range(1, 100)]
    public int WorkerCount { get; set; } = TimingConfiguration.Jobs.DefaultWorkerCount;

    [Range(0, 10)]
    public int RetryAttempts { get; set; } = TimingConfiguration.Jobs.DefaultRetryAttempts;

    public bool DashboardEnabled { get; set; }
}
