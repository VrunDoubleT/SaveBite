using SaveBite.Backend.Configurations;
using SaveBite.Backend.Exceptions;

namespace SaveBite.Backend.Helpers;

public static class FlashDealTiming
{
    // A relative duration must use the same server timestamp as the sale start.
    public static (DateTime End, DateTime Closing) ImmediateWindow(
        DateTime start, int durationMinutes, TimeSpan pickupBuffer)
    {
        if (durationMinutes is < TimingConfiguration.FlashDeals.MinimumDurationMinutes or > TimingConfiguration.FlashDeals.MaximumDurationMinutes)
            throw AppException.BadRequest($"The duration of a flash deal must be between {TimingConfiguration.FlashDeals.MinimumDurationMinutes} and {TimingConfiguration.FlashDeals.MaximumDurationMinutes} minutes.");
        if (pickupBuffer < TimeSpan.Zero)
            throw AppException.BadRequest("Shop closing time must be on or after order end time.");
        var end = start.AddMinutes(durationMinutes);
        return (end, end.Add(pickupBuffer));
    }
}
