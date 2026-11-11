namespace SaveBite.Backend.Models.Requests;

public class NearbyShopsRequest
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double RadiusInKm { get; set; } = 5.0;
    public string? Keyword { get; set; }
    public string? Category { get; set; }
    public string? ShopName { get; set; }
    public string? Distance { get; set; }
    public bool? OnlyOpen { get; set; } = false;
    public bool? OnlyWithFlashDeals { get; set; } = false;
    public bool? HasFlashDeals { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;

    public bool IsFlashDealsOnly => (OnlyWithFlashDeals == true) || (HasFlashDeals == true);
}

public class StoreReviewsQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? Rating { get; set; }
}
