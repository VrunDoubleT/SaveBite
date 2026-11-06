namespace SaveBite.Backend.Services.Interfaces;

// Deliver shared event envelopes privately to an authenticated account.
public interface IRealtimePublisher
{
    Task SendToUserAsync(Guid userId, string eventType, object data);
    Task SendToAllAsync(string eventType, object data);
    Task SendToGroupAsync(string groupName, string eventType, object data);
}
