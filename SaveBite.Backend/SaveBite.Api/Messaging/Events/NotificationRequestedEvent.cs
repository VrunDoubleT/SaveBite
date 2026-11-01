using SaveBite.Backend.Models.DTOs;
namespace SaveBite.Backend.Messaging.Events;

// Keep the request identity stable across broker retries and message redelivery.
public sealed record NotificationRequestedEvent(Guid RequestId, NotificationTarget Target, CreateNotificationCommand Command);
