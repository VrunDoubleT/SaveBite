using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaveBite.Backend.Data;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.Payments.Webhooks;

public sealed class PayOsWebhookSyncWorker(IServiceScopeFactory scopes, IOptions<PayOsWebhookSyncOptions> options,
    TimeProvider clock, ILogger<PayOsWebhookSyncWorker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (configuration["Checkout:PaymentProvider"] == "Simulated") return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ScanAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError("PayOS webhook scan failed: {ErrorType}.", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(options.Value.PollSeconds), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    public async Task ScanAsync(CancellationToken ct)
    {
        await ProcessAccountAsync(PayOsAccount.Platform, ct);
        var cursor = 0;
        while (true)
        {
            List<(int Id, Guid ShopId)> batch;
            await using (var scope = scopes.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var rows = await db.ShopPaymentConfigs.AsNoTracking().Where(x => x.Id > cursor
                    && x.PayosClientId != "" && x.PayosApiKey != "" && x.PayosChecksumKey != "")
                    .OrderBy(x => x.Id).Select(x => new { x.Id, x.ShopId }).Take(options.Value.BatchSize).ToListAsync(ct);
                batch = rows.Select(x => (x.Id, x.ShopId)).ToList();
            }
            if (batch.Count == 0) break;
            await Parallel.ForEachAsync(batch, new ParallelOptions { MaxDegreeOfParallelism = options.Value.MaxConcurrency, CancellationToken = ct },
                async (row, token) => await ProcessAccountAsync(PayOsAccount.Shop(row.ShopId), token));
            cursor = batch[^1].Id;
        }
    }

    private async Task ProcessAccountAsync(string accountId, CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<PayOsWebhookSynchronizer>().SynchronizeAsync(accountId, ct);
            if (accountId == PayOsAccount.Platform)
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var availability = scope.ServiceProvider.GetRequiredService<PlatformFeePaymentAvailability>();
                if (await availability.IsReadyAsync(ct)
                    && await db.PlatformFeeStatements.AnyAsync(x => x.PaymentPauseReason != null && x.TotalFeeAmount > 0
                        && x.Status != SaveBite.Backend.Models.Enums.PlatformFeeStatementStatus.Paid, ct))
                {
                    var revision = await db.PlatformPayOsConfigs.Select(x => (long?)x.Revision).SingleOrDefaultAsync(ct) ?? 0;
                    await availability.ResumeAsync(revision, ct);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogWarning("Webhook processing failed for {AccountId}: {ErrorType}.", accountId, ex.GetType().Name); }
    }
}
