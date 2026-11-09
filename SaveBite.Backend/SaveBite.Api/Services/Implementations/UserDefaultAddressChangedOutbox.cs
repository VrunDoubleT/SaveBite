using System.Text.Json;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Services.Implementations;

public static class UserDefaultAddressChangedOutbox
{
    public static void Stage(
        AppDbContext db,
        Guid userId,
        Guid? addressId,
        double latitude,
        double longitude,
        long addressVersion,
        bool isDefault,
        bool isDeleted = false)
    {
        var evt = new UserDefaultAddressChangedEvent(
            userId,
            addressId,
            latitude,
            longitude,
            addressVersion,
            isDefault,
            isDeleted);

        var outbox = new OrderEventOutbox
        {
            Id = Guid.NewGuid(),
            OrderId = null,
            EventKey = $"user-default-address:{userId:N}:{addressVersion}",
            RoutingKey = RabbitMqRoutingKeys.UserDefaultAddressChanged,
            PayloadJson = JsonSerializer.Serialize(evt),
            CreatedAt = DateTime.UtcNow
        };

        db.Set<OrderEventOutbox>().Add(outbox);
    }
}
