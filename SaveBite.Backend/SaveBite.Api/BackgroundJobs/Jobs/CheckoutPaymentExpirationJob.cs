using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Run payment reconciliation when the scheduled payment window ends.
public sealed class CheckoutPaymentExpirationJob
{
    private readonly ICheckoutPaymentService _payments;

    public CheckoutPaymentExpirationJob(ICheckoutPaymentService payments)
    {
        _payments = payments;
    }

    // Reconcile only the scheduled order; unresolved provider responses throw so Hangfire can retry.
    public Task ExecuteAsync(Guid orderId, CancellationToken ct)
        => _payments.ExpireAsync(orderId, ct);
}
