using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class StaffActivityLog
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid ActionBy { get; set; }
    public Guid? OrderId { get; set; }

    public OrderStatus Action { get; set; } = OrderStatus.Confirmed;
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public Shop Shop { get; set; } = null!;
    public User ActionByUser { get; set; } = null!;
    public Order? Order { get; set; }
}
