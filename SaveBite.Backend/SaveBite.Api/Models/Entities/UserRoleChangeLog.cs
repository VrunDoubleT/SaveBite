using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class UserRoleChangeLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public UserRole PreviousRole { get; set; }

    public UserRole NewRole { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
