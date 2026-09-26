using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class AccountStatusLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AdminId { get; set; }

    public UserStatus Action { get; set; } = UserStatus.Suspended;
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public User Admin { get; set; } = null!;
}
