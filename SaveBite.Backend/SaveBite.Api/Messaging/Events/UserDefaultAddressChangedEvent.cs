namespace SaveBite.Backend.Messaging.Events;

public sealed record UserDefaultAddressChangedEvent(
    Guid UserId,
    Guid? AddressId,
    double Latitude,
    double Longitude,
    long AddressVersion,
    bool IsDefault,
    bool IsDeleted = false);
