namespace SaveBite.Backend.Models.Requests;

public enum CursorMode
{
    Initial = 0,
    Older = 1,
    Newer = 2
}

public class FlashDealCursorRequest
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double RadiusInKm { get; set; } = 15.0;
    
    public DateTime? Cursor1 { get; set; }

    public DateTime? Cursor2 { get; set; }

    public CursorMode Mode { get; set; } = CursorMode.Initial;

    public int Limit { get; set; } = 9;

    public string? Category { get; set; }
    public string? Distance { get; set; }
    public string? Price { get; set; }
    public string? ShopName { get; set; }
}