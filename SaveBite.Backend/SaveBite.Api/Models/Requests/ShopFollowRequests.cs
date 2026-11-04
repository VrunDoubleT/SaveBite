using System.ComponentModel.DataAnnotations;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Requests;

public sealed class ShopNotificationSettingsRequest
{
    [Required]
    [EnumDataType(typeof(ShopNotificationStatus))]
    public ShopNotificationStatus? NotificationStatus { get; set; }
}
