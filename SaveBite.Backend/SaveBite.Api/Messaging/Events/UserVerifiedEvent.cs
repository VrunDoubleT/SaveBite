namespace SaveBite.Backend.Messaging.Events;

// Notify subscribers after a verified account has been persisted.
public sealed record UserVerifiedEvent(Guid UserId, DateTime VerifiedAt);
