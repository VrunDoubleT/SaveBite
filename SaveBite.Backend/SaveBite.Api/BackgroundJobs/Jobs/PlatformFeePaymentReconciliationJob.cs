using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

/// <summary>
/// Authoritative PayOS reconciliation for pending platform fee payments (BR-097, BR-167, BR-195).
/// Recovers missed webhooks and updates expired/cancelled links without expiring on time alone.
/// </summary>
public sealed class PlatformFeePaymentReconciliationJob(
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<PlatformFeePaymentReconciliationJob> logger)
{
    [DisableConcurrentExecution(TimingConfiguration.Jobs.ConcurrencyLockTimeoutSeconds)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await RecoveryBatch.RunAsync(scopes, logger, "fee-payment-reconciliation",
            db => db.PlatformFeePayments.AsNoTracking()
                .Where(p => p.Status == PaymentStatus.Pending && p.GatewayTransactionId != null
                    && (p.CreatedAt <= now.AddMinutes(-1) || (p.GatewayExpiresAt != null && p.GatewayExpiresAt <= now)))
                .Select(p => p.Id),
            (sp, id) => sp.GetRequiredService<IPlatformFeeStatementService>().ReconcilePendingPaymentAsync(id, ct), ct);
    }
}
