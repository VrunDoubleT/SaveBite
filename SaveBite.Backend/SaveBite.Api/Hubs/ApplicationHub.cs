using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SaveBite.Backend.Authorization;

namespace SaveBite.Backend.Hubs;

// Share one authenticated connection across features; clients cannot select another user's channel.
[Authorize(Policy = AuthorizationPolicies.AccountOnly)]
public sealed class ApplicationHub : Hub<IApplicationClient>
{
    public const string Path = "/api/realtime";
}
