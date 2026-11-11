using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

/// <summary>
/// Automatically calculates and aggregates monthly platform fee statements for each shop (BR-093, BR-192).
/// Runs on the 1st day of every month at night (e.g. 01:00 AM) to calculate statements for the preceding month.
/// </summary>
public sealed class PlatformFeeMonthlyStatementJob
{
    private readonly IPlatformFeeStatementService _feeStatementService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PlatformFeeMonthlyStatementJob> _logger;

    public PlatformFeeMonthlyStatementJob(
        IPlatformFeeStatementService feeStatementService,
        TimeProvider timeProvider,
        ILogger<PlatformFeeMonthlyStatementJob> logger)
    {
        _feeStatementService = feeStatementService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Executing PlatformFeeMonthlyStatementJob...");
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        // When running on day 1 of the month, statements are generated for the preceding billing month
        var previousPeriod = now.AddMonths(-1);
        var billingYear = previousPeriod.Year;
        var billingMonth = (short)previousPeriod.Month;

        var statementsCreated = await _feeStatementService.GenerateMonthlyStatementsAsync(billingYear, billingMonth, ct);
        _logger.LogInformation("PlatformFeeMonthlyStatementJob completed. Created/Updated {Count} statement(s) for period {Year}-{Month}.",
            statementsCreated, billingYear, billingMonth);
    }
}
