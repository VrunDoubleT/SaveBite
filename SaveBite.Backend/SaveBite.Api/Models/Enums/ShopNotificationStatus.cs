using System.Text.Json.Serialization;

namespace SaveBite.Backend.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShopNotificationStatus
{
    Disabled = 0,
    Enabled = 1
}
