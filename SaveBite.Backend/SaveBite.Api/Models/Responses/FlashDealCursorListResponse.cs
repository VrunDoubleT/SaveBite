namespace SaveBite.Backend.Models.Responses;

public class FlashDealCursorListResponse
{
    public List<FlashDealResponse> Deals { get; set; } = new();

    public DateTime? Cursor1 { get; set; }

    public DateTime? Cursor2 { get; set; }

    public bool HasOlder { get; set; }

    public bool HasNewer { get; set; }

    // Count new deals prepended to the list when the client requests mode=Older.
    // The frontend uses this count to insert the new deals at the beginning.
    public int PrependedCount { get; set; }

    // Count new deals found while polling with mode=Newer that have not been loaded yet.
    public int NewDealsCount { get; set; }
}
