namespace SaveBite.Backend.Helpers;

// Resolve the Vietnam calendar date used by shop pickup codes.
public static class OrderBusinessDate
{
    // Convert a UTC timestamp to the Vietnam business date used for daily pickup codes.
    public static DateOnly FromUtc(DateTime utc) => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh")));

    // Convert the start (00:00) of a Vietnam calendar day to its UTC instant, used for inclusive-from / exclusive-to date filters.
    public static DateTime StartOfDayUtc(DateOnly date) => TimeZoneInfo.ConvertTimeToUtc(
        date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"));
}
