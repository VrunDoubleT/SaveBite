using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Models.DTOs;

public sealed class ShopMonthlyPlatformFeeReportData
{
    public int OrderCount { get; init; }
    public decimal TotalGrossAmount { get; init; }
    public decimal TotalFeeAmount { get; init; }
    public decimal UnbilledFeeAmount { get; init; }
    public PagedResult<ShopMonthlyPlatformFeeOrderData> Orders { get; init; } = new();
}

public sealed class ShopMonthlyPlatformFeeOrderData
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public Guid FeeConfigId { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal FeeRate { get; init; }
    public decimal OriginalFeeAmount { get; init; }
    public decimal RefundAdjustmentAmount { get; init; }
    public decimal FeeAmount { get; init; }
    public DateTime CalculatedAt { get; init; }
    public Guid? StatementId { get; init; }
}
