using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public class ShopStaff
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public ShopStaffStatus Status { get; set; } = ShopStaffStatus.Active;

    public DateTime JoinedAt { get; set; }

    public Shop Shop { get; set; } = null!;
    public User User { get; set; } = null!;
}
