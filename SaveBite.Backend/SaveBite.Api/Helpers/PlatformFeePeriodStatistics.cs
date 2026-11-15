using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Helpers;

public static class PlatformFeePeriodStatistics
{
    public static PlatformFeePeriodStatisticsResponse Calculate(
        IEnumerable<PlatformFeeStatement> statements, int? year, int? month, DateTime now)
    {
        var bills = statements.Where(s => (!year.HasValue || s.BillingYear == year) && (!month.HasValue || s.BillingMonth == month)
            && s.Status != PlatformFeeStatementStatus.Draft).ToList();
        var paid = bills.Where(s => s.Status == PlatformFeeStatementStatus.Paid && s.TotalFeeAmount > 0).ToList();
        var unpaid = bills.Where(s => s.Status != PlatformFeeStatementStatus.Paid && s.TotalFeeAmount > 0).ToList();
        var overdue = unpaid.Where(s => s.PaymentPauseReason == null
            && (s.Status == PlatformFeeStatementStatus.Overdue || s.DueAt <= now)).ToList();
        return new(year ?? 0, month ?? 0, bills.Sum(s => s.TotalFeeAmount), bills.Sum(Collected),
            unpaid.Sum(s => Math.Max(0, s.TotalFeeAmount - Collected(s))), overdue.Sum(s => Math.Max(0, s.TotalFeeAmount - Collected(s))),
            bills.Select(s => s.ShopId).Distinct().Count(), paid.Select(s => s.ShopId).Distinct().Count(),
            unpaid.Select(s => s.ShopId).Distinct().Count(), overdue.Select(s => s.ShopId).Distinct().Count(),
            bills.Where(s => s.TotalFeeAmount == 0).Select(s => s.ShopId).Distinct().Count(), now, bills.Count, paid.Count, unpaid.Count, overdue.Count, bills.Count(s => s.TotalFeeAmount == 0));
    }

    private static decimal Collected(PlatformFeeStatement statement) => statement.Status == PlatformFeeStatementStatus.Paid
        ? statement.TotalFeeAmount
        : Math.Min(statement.TotalFeeAmount, statement.Payments.Where(p => p.Status == PaymentStatus.Succeeded).Sum(p => Math.Max(p.Amount, p.CollectedAmount)));
}
