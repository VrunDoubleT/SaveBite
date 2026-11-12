namespace SaveBite.Backend.Models.Requests;

public enum CursorMode
{
    Initial = 0,
    Older = 1,
    Newer = 2
}

public class FlashDealCursorRequest
{
    // Optional targeted lookup using the same visibility and feed filters.
    public Guid? DealId { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double RadiusInKm { get; set; } = 5.0;

    // Keyset Cursor Token (Section 8.2)
    public string? Cursor { get; set; }

    // Legacy fields for backward compatibility
    public DateTime? Cursor1 { get; set; }
    public DateTime? Cursor2 { get; set; }
    public CursorMode Mode { get; set; } = CursorMode.Initial;

    public int Limit { get; set; } = 9;

    public string? Category { get; set; }
    public string? Distance { get; set; }
    public string? Price { get; set; }
    public string? ShopName { get; set; }
    public string? Keyword { get; set; }
    public string? LocationMode { get; set; } // "gps", "saved_address", "default"
}
