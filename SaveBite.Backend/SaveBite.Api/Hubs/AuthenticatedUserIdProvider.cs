using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SaveBite.Backend.Hubs;

// Normalize JWT user IDs so every connection for one account receives the same private events.
public sealed class AuthenticatedUserIdProvider : IUserIdProvider
{
    // Normalize the authenticated account ID so private events reach all its connections.
    public string? GetUserId(HubConnectionContext connection)
    {
        var value = connection.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? connection.User?.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id.ToString("D") : null;
    }
}
