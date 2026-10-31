using System.Globalization;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Helpers;

/// <summary>Pure calculation helpers shared by the store (FE-17) and platform (FE-23) dashboards.</summary>
public static class AnalyticsCalculator
{
    // AwaitingPayment orders are unsubmitted checkouts and are never reported (see ShopOrderVisibility).
    public static readonly OrderStatus[] ReportedOrderStatuses =
    {
        OrderStatus.Completed, OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Preparing,
        OrderStatus.ReadyForPickup, OrderStatus.Cancelled, OrderStatus.Rejected, OrderStatus.Expired, OrderStatus.NoShow
    };

    public static AnalyticsPeriodInfoResponse DescribePeriod(AnalyticsPeriod period) => new(
        period.Preset,
        period.Granularity.ToString().ToLowerInvariant(),
        period.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        period.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        period.FromUtc,
        period.ToUtcInclusive);

    /// <summary>
    /// Deals grouped by the status they actually have. The five statuses the app really assigns are always listed;
    /// SoldOut is listed only when present because the app never persists it.
    /// </summary>
    public static IReadOnlyList<StatusMetricItemResponse> BuildDealStatusBreakdown(
        IReadOnlyList<DealStatusCountRow> rows,
        int total)
    {
        return Enum.GetValues<FlashDealStatus>()
            .Select(status => new
            {
                Status = status,
                Count = rows.Where(r => r.Status == status).Sum(r => r.Count)
            })
            .Where(item => item.Status != FlashDealStatus.SoldOut || item.Count > 0)
            .Select(item => new StatusMetricItemResponse(item.Status.ToString(), item.Count, 0m, Percent(item.Count, total)))
            .ToList();
    }

    public static decimal Percent(decimal part, decimal total)
        => total > 0m ? Math.Round(part * 100m / total, 1) : 0m;

    public static IReadOnlyList<OrderStatusCountItemResponse> BuildOrderStatusBreakdown(IReadOnlyList<OrderStatusCountRow> rows)
    {
        var total = rows.Sum(r => r.Count);
        var counts = rows.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Sum(r => r.Count));

        return ReportedOrderStatuses
            .Select(status =>
            {
                var count = counts.GetValueOrDefault(status);
                return new OrderStatusCountItemResponse(status.ToString(), count, Percent(count, total));
            })
            .ToList();
    }

    public static (decimal TotalAmount, int TotalCount, IReadOnlyList<PaymentMethodBreakdownItemResponse> Methods)
        BuildPaymentMethods(IReadOnlyList<PaymentMethodAmountRow> rows)
    {
        var totalAmount = rows.Sum(r => r.Amount);
        var totalCount = rows.Sum(r => r.Count);
        var byMethod = rows.GroupBy(r => r.Method)
            .ToDictionary(g => g.Key, g => (Count: g.Sum(r => r.Count), Amount: g.Sum(r => r.Amount)));

        // Every PaymentMethod value is listed, so a newly added method can never silently drop out of the breakdown.
        var methods = Enum.GetValues<PaymentMethod>()
            .Select(method =>
            {
                var item = byMethod.GetValueOrDefault(method);
                return new PaymentMethodBreakdownItemResponse(method.ToString(), item.Count, item.Amount, Percent(item.Amount, totalAmount));
            })
            .ToList();

        return (totalAmount, totalCount, methods);
    }

    /// <summary>
    /// Revenue trend bucketed by hour (a single day), day (a month or range) or month (a year). Buckets follow the
    /// Vietnam calendar. Net of a bucket = paid - refunded (BR-114). The all-time view lists only months with activity.
    /// </summary>
    public static IReadOnlyList<RevenueTrendPointResponse> BuildRevenueTrend(
        IReadOnlyList<CompletedOrderFinancialRow> rows,
        AnalyticsPeriod period)
    {
        var buckets = new Dictionary<string, (decimal Online, decimal Cash, decimal Refunded, int Count)>();
        foreach (var row in rows)
        {
            var key = BucketKey(row.CompletedAtUtc, period.Granularity);
            buckets.TryGetValue(key, out var current);
            buckets[key] = (
                current.Online + row.PaidOnlineAmount,
                current.Cash + row.PaidCashAmount,
                current.Refunded + row.RefundedAmount,
                current.Count + 1);
        }

        RevenueTrendPointResponse ToPoint(string key, (decimal Online, decimal Cash, decimal Refunded, int Count) v)
            => new(key, v.Online + v.Cash, v.Refunded, v.Online + v.Cash - v.Refunded, v.Count, v.Online, v.Cash);

        var keys = DenseKeys(period) ?? buckets.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        return keys.Select(key => ToPoint(key, buckets.GetValueOrDefault(key))).ToList();
    }

    public static IReadOnlyList<DailyCountPointResponse> BuildCountTrend(
        IEnumerable<DateTime> utcTimestamps,
        AnalyticsPeriod period)
    {
        var counts = utcTimestamps
            .GroupBy(timestamp => BucketKey(timestamp, period.Granularity))
            .ToDictionary(g => g.Key, g => g.Count());

        var keys = DenseKeys(period) ?? counts.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        return keys.Select(key => new DailyCountPointResponse(key, counts.GetValueOrDefault(key))).ToList();
    }

    private static string BucketKey(DateTime utc, TrendGranularity granularity)
    {
        var local = VietnamTime.ToLocal(utc);
        return granularity switch
        {
            TrendGranularity.Hour => local.ToString("yyyy-MM-dd'T'HH", CultureInfo.InvariantCulture),
            TrendGranularity.Month => local.ToString("yyyy-MM-'01'", CultureInfo.InvariantCulture),
            _ => local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }

    /// <summary>Every bucket of the period, so empty hours/days/months show as zero. Null for the all-time view.</summary>
    private static List<string>? DenseKeys(AnalyticsPeriod period)
    {
        if (period.IsAllTime) return null;

        var keys = new List<string>();
        switch (period.Granularity)
        {
            case TrendGranularity.Hour:
                var day = period.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                for (var hour = 0; hour < 24; hour++)
                    keys.Add($"{day}T{hour:00}");
                break;

            case TrendGranularity.Month:
                for (var month = new DateOnly(period.StartDate.Year, period.StartDate.Month, 1);
                     month <= period.EndDate;
                     month = month.AddMonths(1))
                    keys.Add(month.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;

            default:
                for (var date = period.StartDate; date <= period.EndDate; date = date.AddDays(1))
                    keys.Add(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;
        }

        return keys;
    }
}
