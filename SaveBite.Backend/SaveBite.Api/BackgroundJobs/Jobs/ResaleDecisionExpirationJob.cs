using SaveBite.Backend.Services.Implementations;
namespace SaveBite.Backend.BackgroundJobs.Jobs;
public sealed class ResaleDecisionExpirationJob(OrderResaleService resale)
{
    public Task ExecuteAsync(Guid orderId, CancellationToken ct) => resale.ExpireAsync(orderId, ct);
}
