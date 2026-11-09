using SaveBite.Backend.Configurations;
namespace SaveBite.Backend.Helpers;

public static class PickupReminderPolicy
{
    public static DateTime DueAt(DateTime confirmedAt, DateTime hardDeadline)
    {
        var standard = hardDeadline.AddMinutes(-TimingConfiguration.Orders.PickupReminderLeadMinutes);
        return standard > confirmedAt ? standard : confirmedAt.AddTicks((hardDeadline - confirmedAt).Ticks / 2);
    }
}
