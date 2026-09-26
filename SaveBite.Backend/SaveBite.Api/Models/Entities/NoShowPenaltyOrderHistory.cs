namespace SaveBite.Backend.Models.Entities;

public class NoShowPenaltyOrderHistory
{
    public Guid Id { get; set; }

    public Guid NoShowPenaltyId { get; set; }

    public Guid OrderId { get; set; }
    
    public decimal OrderValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public NoShowPenalty NoShowPenalty { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
