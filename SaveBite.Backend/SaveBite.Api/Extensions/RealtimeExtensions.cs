using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http.Connections;
using SaveBite.Backend.Hubs;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.Extensions;

// Register and expose the shared authenticated realtime infrastructure.
public static class RealtimeExtensions
{
    // Register the shared SignalR hub services, user identity provider and event publisher.
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddHttpContextAccessor();
        services.AddSingleton<IUserIdProvider, AuthenticatedUserIdProvider>();
        services.AddSingleton<IRealtimePublisher, SignalRRealtimePublisher>();
        return services;
    }

    // Expose the authenticated hub at the shared realtime endpoint.
    public static IEndpointRouteBuilder MapRealtime(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ApplicationHub>(ApplicationHub.Path, options =>
        {
            options.CloseOnAuthenticationExpiration = true;
            options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents;
        });
        return endpoints;
    }
}
