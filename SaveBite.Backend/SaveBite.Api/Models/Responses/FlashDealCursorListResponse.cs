namespace SaveBite.Backend.Models.Responses;

public class FlashDealCursorListResponse
{
    public List<FlashDealResponse> Deals { get; set; } = new();

    // Keyset Cursor Token (Section 8.2)
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }

    // Legacy fields for backward compatibility with existing components
    public DateTime? Cursor1 { get; set; }
    public DateTime? Cursor2 { get; set; }
    public bool HasOlder { get; set; }
    public bool HasNewer { get; set; }
    public int PrependedCount { get; set; }
    public int NewDealsCount { get; set; }
}
