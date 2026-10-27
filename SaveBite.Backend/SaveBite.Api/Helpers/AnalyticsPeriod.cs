using System.Globalization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Helpers;

/// <summary>Vietnam business calendar (UTC+7, no DST). All dashboards and audit filters use the same day boundaries.</summary>
public static class VietnamTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
    }

    /// <summary>The wall-clock time in Vietnam for a UTC instant.</summary>
    public static DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateOnly ToDate(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));

    /// <summary>The UTC instant at which the given Vietnam calendar day starts.</summary>
    public static DateTime StartOfDayUtc(DateOnly date)
        => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), Zone);
}

/// <summary>How a trend chart is bucketed: a single day by hour, a month or range by day, a year by month.</summary>
public enum TrendGranularity
{
    Hour,
    Day,
    Month
}

/// <summary>
/// A resolved reporting window. Dates are Vietnam calendar days (inclusive); the UTC range is half-open:
/// <c>FromUtc &lt;= t &lt; ToUtcExclusive</c>, so no tick is ever double counted or lost at a boundary.
/// </summary>
public sealed record AnalyticsPeriod(
    DateOnly StartDate,
    DateOnly EndDate,
    DateTime FromUtc,
    DateTime ToUtcExclusive,
    string Preset,
    TrendGranularity Granularity,
    bool IsAllTime = false)
{
    public DateTime ToUtcInclusive => ToUtcExclusive.AddTicks(-1);
    public int TotalDays => EndDate.DayNumber - StartDate.DayNumber + 1;
}

public static class AnalyticsPeriodResolver
{
    /// <summary>A custom from/to range is limited to one month.</summary>
    public const int MaxCustomRangeDays = 31;

    private static readonly DateOnly AllTimeStart = new(2000, 1, 1);

    public static AnalyticsPeriod Resolve(AnalyticsPeriodRequest request, DateTime nowUtc)
    {
        var today = VietnamTime.ToDate(nowUtc);

        if (request.FromDate.HasValue || request.ToDate.HasValue)
        {
            var end = request.ToDate ?? today;
            var start = request.FromDate ?? end;

            if (start > end)
                throw AppException.BadRequest("FromDate cannot be later than ToDate.");

            if (end.DayNumber - start.DayNumber + 1 > MaxCustomRangeDays)
                throw AppException.BadRequest(
                    $"A custom range can cover at most {MaxCustomRangeDays} days. Use the month or year view for longer periods.");

            return Build(start, end, "custom");
        }

        var preset = (request.Preset ?? "today").Trim().ToLowerInvariant();
        switch (preset)
        {
            case "":
            case "today":
                return Build(today, today, "today");

            case "yesterday":
                return Build(today.AddDays(-1), today.AddDays(-1), "yesterday");

            case "7d":
                return Build(today.AddDays(-6), today, "7d");

            case "month":
            {
                var first = ParseMonth(request.Month, today);
                if (first > today)
                    throw AppException.BadRequest("The selected month is in the future.");
                var last = first.AddMonths(1).AddDays(-1);
                return Build(first, last < today ? last : today, "month");
            }

            case "year":
            {
                var year = request.Year ?? today.Year;
                if (year < AllTimeStart.Year || year > today.Year)
                    throw AppException.BadRequest($"Year must be between {AllTimeStart.Year} and {today.Year}.");
                var first = new DateOnly(year, 1, 1);
                var last = new DateOnly(year, 12, 31);
                return Build(first, last < today ? last : today, "year", TrendGranularity.Month);
            }

            default:
                throw AppException.BadRequest("Preset must be one of: today, yesterday, 7d, month, year.");
        }
    }

    /// <summary>Everything the shop or platform has ever recorded, shown next to the selected period.</summary>
    public static AnalyticsPeriod AllTime(DateTime nowUtc)
    {
        var today = VietnamTime.ToDate(nowUtc);
        return new AnalyticsPeriod(
            AllTimeStart,
            today,
            VietnamTime.StartOfDayUtc(AllTimeStart),
            VietnamTime.StartOfDayUtc(today.AddDays(1)),
            "all",
            TrendGranularity.Month,
            IsAllTime: true);
    }

    private static AnalyticsPeriod Build(DateOnly start, DateOnly end, string preset, TrendGranularity? granularity = null)
    {
        var resolved = granularity
                       ?? (start == end ? TrendGranularity.Hour : TrendGranularity.Day);

        return new AnalyticsPeriod(
            start,
            end,
            VietnamTime.StartOfDayUtc(start),
            VietnamTime.StartOfDayUtc(end.AddDays(1)),
            preset,
            resolved);
    }

    private static DateOnly ParseMonth(string? month, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(month))
            return new DateOnly(today.Year, today.Month, 1);

        if (!DateOnly.TryParseExact($"{month.Trim()}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed) || parsed.Year < AllTimeStart.Year)
            throw AppException.BadRequest("Month must look like 2026-10.");

        return parsed;
    }
}
