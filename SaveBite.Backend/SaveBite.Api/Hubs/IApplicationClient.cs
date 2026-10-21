using SaveBite.Backend.Models.DTOs;

namespace SaveBite.Backend.Hubs;

// Define the shared event envelope delivered to authenticated SignalR clients.
public interface IApplicationClient
{
    Task ReceiveEvent(RealtimeEvent message);
}
