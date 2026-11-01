namespace SaveBite.Backend.Models.Requests;

/// <summary>
/// Reporting window. Default is today. Preset: today | yesterday | 7d | month | year.
/// Month ("yyyy-MM") and Year pick which calendar month/year the month/year presets show.
/// A custom FromDate/ToDate range may cover at most 31 days.
/// </summary>
public sealed record AnalyticsPeriodRequest(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? Preset = "today",
    string? Month = null,
    int? Year = null
);