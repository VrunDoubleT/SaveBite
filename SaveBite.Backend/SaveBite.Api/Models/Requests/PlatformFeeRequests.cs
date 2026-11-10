using System.ComponentModel.DataAnnotations;
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

public sealed class GetShopMonthlyPlatformFeeReportRequest
{
    [Range(2000, 9999)]
    public int Year { get; set; }

    [Range(1, 12)]
    public short Month { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    public int PageNumber { get => Page; set => Page = value; }

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
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