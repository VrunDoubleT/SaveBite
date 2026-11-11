using SaveBite.Backend.Audit;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.Messaging.Consumer.ProductWarnings;

// The warning table is the durable schedule. Rechecking predicates makes retries safe.
public sealed class ProductWarningDeadlineConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductWarningDeadlineConsumer> _logger;

    public ProductWarningDeadlineConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductWarningDeadlineConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;
                var audit = scope.ServiceProvider.GetRequiredService<IAuditWriter>();
                var warnings = await db.ProductWarnings.AsNoTracking()
                    .Where(WarningDealPolicy.BlockedAt(now)).Where(w => !w.DealBlocked)
                    .Select(w => new { w.Id, w.ProductId, w.ShopId }).ToListAsync(stoppingToken);
                foreach (var warning in warnings)
                {
                    await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
                    var changed = await db.ProductWarnings.Where(w => w.Id == warning.Id)
                        .Where(WarningDealPolicy.BlockedAt(now)).Where(w => !w.DealBlocked)
                        .ExecuteUpdateAsync(s => s.SetProperty(w => w.DealBlocked, true), stoppingToken);
                    if (changed == 1)
                    {
                        audit.Record("ProductWarning", warning.Id, "DEADLINE_BLOCKED",
                            new { DealBlocked = false }, new { DealBlocked = true, warning.ProductId, warning.ShopId },
                            "Correction deadline passed without an approved correction.");
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    await transaction.CommitAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Warning deadline processing failed; retrying.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
