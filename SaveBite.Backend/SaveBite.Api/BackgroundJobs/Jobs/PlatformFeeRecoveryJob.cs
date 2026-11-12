using SaveBite.Backend.Configurations;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

// Hourly catch-up. A paid statement is immutable; unbilled late fees roll into a subsequent unpaid billing period.
public sealed class PlatformFeeRecoveryJob(IServiceScopeFactory scopes, TimeProvider clock, ILogger<PlatformFeeRecoveryJob> logger)
{
    [DisableConcurrentExecution(TimingConfiguration.Jobs.ConcurrencyLockTimeoutSeconds)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var cutoff = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        List<int> periods;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            periods = await db.OrderPlatformFees.Where(f => f.StatementId == null && f.CalculatedAt < cutoff)
                .Select(f => f.CalculatedAt.Year * 100 + f.CalculatedAt.Month).Concat(db.PlatformFeeAdjustments.Where(a => a.StatementId == null && a.CreatedAt < cutoff)
                    .Select(a => a.CreatedAt.Year * 100 + a.CreatedAt.Month)).Distinct().ToListAsync(ct);
        }
        await RecoveryBatch.RunAsync(scopes, logger, "fee-notifications", db => db.PlatformFeeStatements
            .Where(s => s.Status != SaveBite.Backend.Models.Enums.PlatformFeeStatementStatus.Draft).Select(s => s.Id),
            (sp, id) => sp.GetRequiredService<IPlatformFeeStatementService>().RepairNotificationsAsync(id, ct), ct);
        await RecoveryBatch.RunAsync(scopes, logger, "fee-payment-reconciliation", db => db.PlatformFeePayments.AsNoTracking()
            .Where(p => p.Status == SaveBite.Backend.Models.Enums.PaymentStatus.Pending && p.GatewayTransactionId != null).Select(p => p.Id),
            (sp, id) => sp.GetRequiredService<IPlatformFeeStatementService>().ReconcilePendingPaymentAsync(id, ct), ct);
        // Include previous month to pick up notifications and late items after older paid statements.
        foreach (var period in periods.Append(cutoff.AddMonths(-1).Year * 100 + cutoff.AddMonths(-1).Month).Distinct().OrderBy(p => p))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IPlatformFeeStatementService>()
                    .GenerateMonthlyStatementsAsync(period / 100, (short)(period % 100), ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            { logger.LogError(ex, "Fee catch-up failed for {Year}/{Month}.", period / 100, period % 100); }
        }
    }
}
