using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

public sealed class ShopFollow
{
    public Guid CustomerId { get; set; }
    public Guid ShopId { get; set; }
    public ShopNotificationStatus NotificationStatus { get; set; } = ShopNotificationStatus.Enabled;
    public DateTime FollowedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User Customer { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
}
