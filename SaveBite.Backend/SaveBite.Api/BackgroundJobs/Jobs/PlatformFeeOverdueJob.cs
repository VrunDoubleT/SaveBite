using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.BackgroundJobs.Jobs;

/// <summary>
/// Scans for overdue platform fee statements and suspends delinquent shops (BR-095, BR-193).
/// </summary>
public sealed class PlatformFeeOverdueJob
{
    private readonly IPlatformFeeStatementService _feeStatementService;
    private readonly ILogger<PlatformFeeOverdueJob> _logger;

    public PlatformFeeOverdueJob(
        IPlatformFeeStatementService feeStatementService,
        ILogger<PlatformFeeOverdueJob> logger)
    {
        _feeStatementService = feeStatementService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Executing PlatformFeeOverdueJob check...");
        var suspendedCount = await _feeStatementService.CheckAndSuspendOverdueShopsAsync(ct);
        _logger.LogInformation("PlatformFeeOverdueJob check completed. Suspended {Count} delinquent shop(s).", suspendedCount);
    }
}
