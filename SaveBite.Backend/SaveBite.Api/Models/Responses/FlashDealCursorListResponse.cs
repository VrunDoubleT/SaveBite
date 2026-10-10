namespace SaveBite.Backend.Models.Responses;

public class FlashDealCursorListResponse
{
    public List<FlashDealResponse> Deals { get; set; } = new();
    
    public DateTime? Cursor1 { get; set; }
    
    public DateTime? Cursor2 { get; set; }
    
    public bool HasOlder { get; set; }
    
    public bool HasNewer { get; set; }
    
    /// <summary>
    /// Số lượng deal mới (mới hơn cursor2 cũ) được prepend vào đầu danh sách
    /// khi client gọi mode=Older. Frontend dùng để biết cần insert lên đầu.
    /// </summary>
    public int PrependedCount { get; set; }
    
    /// <summary>
    /// Số lượng deal mới tìm thấy khi polling (mode=Newer), chưa được load vào trang.
    /// </summary>
    public int NewDealsCount { get; set; }
}