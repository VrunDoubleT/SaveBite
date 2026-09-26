using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class OrderStatusHistory
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }

    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }

    public Guid? ChangedBy { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
