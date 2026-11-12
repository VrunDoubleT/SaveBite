namespace SaveBite.Backend.Models.Responses;

public sealed class PlatformFeeConfigResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Reason { get; set; }
    public Guid? ReplacesConfigId { get; set; }
    public Guid? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosureReason { get; set; }
    public DateTime? ActivationProcessedAt { get; set; }
    public string? CreatedByName { get; set; }
    public int MaxFutureSchedules { get; set; } = 5;
    public decimal FeeRate { get; set; }
    public decimal FeePercentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public PlatformFeeConfigResponse() { }

    public PlatformFeeConfigResponse(
        Guid id,
        decimal feeRate,
        decimal feePercentage,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        Guid? createdBy,
        DateTime createdAt)
    {
        Id = id;
        FeeRate = feeRate;
        FeePercentage = feePercentage;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }
}