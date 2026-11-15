namespace SaveBite.Backend.Helpers;

// Fees are payable in whole VND; use the same rule for accruals, credits and bills.
public static class PlatformFeeAmounts
{
    public static decimal Round(decimal amount) => decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
}
