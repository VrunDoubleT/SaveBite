namespace SaveBite.Backend.Constants;

// FIX-25: single source of truth for the minimum VND amount accepted anywhere in the system
// (PayOS rejects 0 VND and fractional amounts).
public static class MoneyLimits
{
    public const int MinAmountVnd = 2000;

    public static bool IsWholeVnd(decimal value) => decimal.Truncate(value) == value;
}
