using System.ComponentModel.DataAnnotations;

namespace SaveBite.Backend.Payments.Webhooks;

public sealed class PayOsWebhookSyncOptions
{
    [Range(1, 60)] public int PollSeconds { get; set; } = 5;
    [Range(1, 16)] public int MaxConcurrency { get; set; } = 3;
    [Range(1, 1000)] public int BatchSize { get; set; } = 100;
    [Range(1, 60)] public int RetryBaseSeconds { get; set; } = 5;
    [Range(60, 3600)] public int RetryMaxSeconds { get; set; } = 300;

    public TimeSpan Backoff(int attempts, double jitter)
        => TimeSpan.FromSeconds(Math.Min(RetryMaxSeconds,
            RetryBaseSeconds * Math.Pow(2, Math.Clamp(attempts - 1, 0, 20))) * (0.8 + 0.2 * Math.Clamp(jitter, 0, 1)));
}
