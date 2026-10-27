namespace SaveBite.Backend.Models.Requests;

public sealed class UpdatePlatformFeeConfigRequest
{
    /// <summary>
    /// Fee percentage (e.g., 5.0 for 5%) or decimal fraction (e.g., 0.05).
    /// Value must be between 0 and 100 percent.
    /// </summary>
    public decimal FeeRate { get; set; }

    /// <summary>
    /// Optional effective start time. If not provided or null, takes effect immediately.
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }
    
    /// <summary>
    /// If true, any existing future scheduled configs will be cancelled/superseded.
    /// </summary>
    public bool CancelUpcomingScheduled { get; set; } = false;
}
