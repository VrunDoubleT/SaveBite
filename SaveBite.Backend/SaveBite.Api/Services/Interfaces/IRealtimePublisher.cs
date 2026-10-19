namespace SaveBite.Backend.Services.Interfaces;

// Deliver shared event envelopes privately to an authenticated account.
public interface IRealtimePublisher
{
    Task SendToUserAsync(Guid userId, string eventType, object data);
}
