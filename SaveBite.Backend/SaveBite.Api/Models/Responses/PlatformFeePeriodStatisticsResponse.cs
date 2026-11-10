namespace SaveBite.Backend.Models.Responses;

public sealed record PlatformFeePeriodStatisticsResponse(
    int Year, int Month, decimal TotalAmount, decimal PaidAmount, decimal OutstandingAmount,
    decimal OverdueAmount, int TotalShops, int PaidShops, int UnpaidShops, int OverdueShops,
    int ZeroFeeShops, DateTime AsOf, int TotalStatements, int PaidStatements,
    int UnpaidStatements, int OverdueStatements, int ZeroFeeStatements);
