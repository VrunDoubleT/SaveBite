namespace SaveBite.Backend.Models.Entities;

// A lifetime receipt consumes the order's single customer refund submission, even after withdrawal or rejection.
public class OrderRefundRequest
{
    public Guid OrderId { get; set; }
    public Guid RefundId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Order Order { get; set; } = null!;
    public Refund Refund { get; set; } = null!;
}
