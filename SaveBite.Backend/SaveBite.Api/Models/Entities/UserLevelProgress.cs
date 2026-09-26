namespace SaveBite.Backend.Models.Entities;

public class UserLevelProgress
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public int CompletedOrdersCount { get; set; }

    public decimal AccumulatedOrderValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
