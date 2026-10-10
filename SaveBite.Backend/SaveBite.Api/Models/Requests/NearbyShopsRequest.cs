namespace SaveBite.Backend.Models.Requests;

public class NearbyShopsRequest
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double RadiusInKm { get; set; } = 10.0;
    public string? Keyword { get; set; }
    public bool? OnlyOpen { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
}

public class StoreReviewsQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? Rating { get; set; }
}
