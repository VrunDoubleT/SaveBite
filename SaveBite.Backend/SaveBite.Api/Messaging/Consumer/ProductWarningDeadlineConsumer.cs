using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.Messaging.Consumer;

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
                await db.ProductWarnings
                    .Where(WarningDealPolicy.BlockedAt(now))
                    .Where(w => !w.DealBlocked)
                    .ExecuteUpdateAsync(s => s.SetProperty(w => w.DealBlocked, true), stoppingToken);
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
