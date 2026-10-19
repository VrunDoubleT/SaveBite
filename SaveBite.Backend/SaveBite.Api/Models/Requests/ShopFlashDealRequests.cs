using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Requests;

public class CreateFlashDealRequest
{
    public Guid ProductId { get; set; }
    public bool IsImmediate { get; set; } = false;
    public DateTime? SaleStartTime { get; set; }
    public DateTime OrderEndTime { get; set; }
    public DateTime ShopClosingTime { get; set; }
    public List<CreateFlashDealVariantRequest> Variants { get; set; } = new();
}

public class CreateFlashDealVariantRequest
{
    public Guid VariantId { get; set; }
    public decimal DealPrice { get; set; }
    public int TotalQuantity { get; set; }
}

public class UpdateFlashDealRequest
{
    public DateTime SaleStartTime { get; set; }
    public DateTime OrderEndTime { get; set; }
    public DateTime ShopClosingTime { get; set; }
    public List<UpdateFlashDealVariantRequest> Variants { get; set; } = new();
}

public class UpdateFlashDealVariantRequest
{
    public Guid VariantId { get; set; }
    public decimal DealPrice { get; set; }
    public int TotalQuantity { get; set; }
    public FlashDealVariantStatus Status { get; set; } = FlashDealVariantStatus.Active;
}

public class ShopFlashDealQueryRequest
{
    public FlashDealStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}