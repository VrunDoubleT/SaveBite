using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class PlatformFeeScheduleActivationJob(IPlatformFeeConfigService fees)
{
    public Task ExecuteAsync(CancellationToken ct) => fees.ProcessEffectiveConfigsAsync(ct);
}
