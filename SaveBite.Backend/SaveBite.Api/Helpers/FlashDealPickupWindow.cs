using SaveBite.Backend.Exceptions;

namespace SaveBite.Backend.Helpers;

public static class FlashDealPickupWindow
{
    public const int MaximumExtensionMinutes = 60;

    // Resolve the closing instant of the operating session containing the start, including overnight shops.
    // Equal opening/closing times are the existing convention for a shop open 24/7.
    public static DateTime Latest(DateTime start, DateTime end, TimeOnly? opening, TimeOnly? closing)
    {
        var latest = end.AddMinutes(MaximumExtensionMinutes);
        if (!opening.HasValue || !closing.HasValue || opening == closing) return latest;
        var local = start.AddHours(7);
        var time = TimeOnly.FromDateTime(local);
        var overnight = opening.Value > closing.Value;
        if (overnight ? time < opening.Value && time >= closing.Value : time < opening.Value || time >= closing.Value)
            throw AppException.BadRequest("Flash deal must start during shop operating hours.");
        var close = local.Date.Add(closing.Value.ToTimeSpan());
        if (overnight && time >= opening.Value) close = close.AddDays(1);
        var closeUtc = DateTime.SpecifyKind(close.AddHours(-7), DateTimeKind.Utc);
        if (end > closeUtc) throw AppException.BadRequest("Flash deal must end no later than shop closing time.");
        return latest < closeUtc ? latest : closeUtc;
    }

    public static void Validate(DateTime start, DateTime end, DateTime pickupEnd,
        TimeOnly? opening, TimeOnly? closing, DateTime? committedDeadline = null)
    {
        var latest = Latest(start, end, opening, closing);
        if (pickupEnd < end || pickupEnd > latest)
            throw AppException.BadRequest("Pickup cutoff must be between deal end and 60 minutes afterwards, without exceeding shop closing time.");
        if (committedDeadline.HasValue && pickupEnd < committedDeadline.Value)
            throw AppException.BadRequest($"Pickup cutoff cannot be earlier than an existing order deadline ({committedDeadline.Value.AddHours(7):dd/MM/yyyy HH:mm:ss}).");
    }
}
