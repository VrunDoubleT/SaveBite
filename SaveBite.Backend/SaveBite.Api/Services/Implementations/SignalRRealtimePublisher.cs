using Microsoft.AspNetCore.SignalR;
using SaveBite.Backend.Hubs;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Publish after database commit; a disconnected client reconciles its state when it reconnects.
public sealed class SignalRRealtimePublisher : IRealtimePublisher
{
    private readonly IHubContext<ApplicationHub, IApplicationClient> _hub;
    private readonly TimeProvider _clock;
    private readonly ILogger<SignalRRealtimePublisher> _logger;
    private readonly IHttpContextAccessor? _context;

    public SignalRRealtimePublisher(
        IHubContext<ApplicationHub, IApplicationClient> hub,
        TimeProvider clock,
        ILogger<SignalRRealtimePublisher> logger,
        IHttpContextAccessor? context = null)
    {
        _hub = hub;
        _clock = clock;
        _logger = logger;
        _context = context;
    }

    // Send a private versioned event to every connected tab for one authenticated account.
    public async Task SendToUserAsync(Guid userId, string eventType, object data)
    {
        try
        {
            // This ID identifies an originating HTTP action; it never determines event recipients.
            var header = _context?.HttpContext?.Request.Headers["X-Realtime-Request-Id"].ToString();
            var correlationId = Guid.TryParse(header, out var id) ? id.ToString("D") : null;
            await _hub.Clients.User(userId.ToString("D"))
                .ReceiveEvent(new RealtimeEvent(Guid.NewGuid(), eventType, 1, _clock.GetUtcNow().UtcDateTime, data, correlationId))
                .WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            // A delivery failure must not turn a committed cart change into an HTTP error.
            _logger.LogWarning(ex, "Realtime event {EventType} could not be delivered.", eventType);
        }
    }
}
