using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Helpers;

public static class ShopSuspensionRules
{
    public static bool HasReasons(Shop shop) => shop.SuspendedByAdmin || shop.SuspendedForPlatformFees || shop.SuspendedForOverdueRefunds;
    public static bool ReactivateIfResolved(Shop shop)
    {
        if (shop.Status != ShopStatus.Suspended || HasReasons(shop)) return false;
        shop.Status = ShopStatus.Active;
        return true;
    }
    public static string? Reason(Shop shop)
    {
        var reasons = new List<string>();
        if (shop.SuspendedByAdmin) reasons.Add("Suspended by an administrator.");
        if (shop.SuspendedForPlatformFees) reasons.Add("Overdue platform fees remain unpaid.");
        if (shop.SuspendedForOverdueRefunds) reasons.Add("Overdue mandatory refunds remain unpaid. Pay all overdue mandatory refunds to remove this restriction.");
        return reasons.Count == 0 ? null : string.Join(" ", reasons);
    }
}
