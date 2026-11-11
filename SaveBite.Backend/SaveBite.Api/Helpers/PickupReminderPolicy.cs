namespace SaveBite.Backend.Helpers;

public static class PickupReminderPolicy
{
    public static DateTime DueAt(DateTime confirmedAt, DateTime hardDeadline)
    {
        var standard = hardDeadline.AddMinutes(-10);
        return standard > confirmedAt ? standard : confirmedAt.AddTicks((hardDeadline - confirmedAt).Ticks / 2);
    }
}
