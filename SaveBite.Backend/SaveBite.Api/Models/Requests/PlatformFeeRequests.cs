using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Requests;

public sealed class GetShopPlatformFeesRequest
{
    public int Page { get; set; } = 1;
    public int PageNumber { get => Page; set => Page = value; }
    public int PageSize { get; set; } = 10;
    public int? Year { get; set; }
    public short? Month { get; set; }
    public PlatformFeeStatementStatus? Status { get; set; }
}

public sealed class RecordExternalFeePaymentRequest
{
    public decimal Amount { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Note { get; set; }
}
public sealed class GetAdminPlatformFeesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? Year { get; set; }
    public short? Month { get; set; }
    public PlatformFeeStatementStatus? Status { get; set; }
    public string? Search { get; set; }
}