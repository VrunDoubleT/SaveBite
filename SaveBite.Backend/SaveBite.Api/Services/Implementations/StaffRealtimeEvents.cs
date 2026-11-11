using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Call only after the invitation/membership transaction commits.
public static class StaffRealtimeEvents
{
    public static async Task InvitationChangedAsync(IRealtimePublisher realtime, StaffInvitation invitation, bool staffJoined = false)
    {
        var payload = new { invitationId = invitation.Id, shopId = invitation.ShopId };
        await realtime.SendToUserAsync(invitation.InvitedUserId, RealtimeEvents.StaffInvitationsChanged, payload);
        await realtime.SendToUserAsync(invitation.Shop.OwnerUserId, RealtimeEvents.StaffInvitationsChanged, payload);
        if (staffJoined)
            await realtime.SendToUserAsync(invitation.Shop.OwnerUserId, RealtimeEvents.ShopStaffChanged, payload);
    }
}
