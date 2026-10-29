namespace SaveBite.Backend.Models.Entities;

// Preserve the purchased item and requested quantity without changing the original order item.
public class RefundItem
{
    public Guid Id { get; set; }
    public Guid RefundId { get; set; }
    public Guid OrderItemId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? ImageUrl { get; set; }
    public int PurchasedQuantity { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public Refund Refund { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
}
