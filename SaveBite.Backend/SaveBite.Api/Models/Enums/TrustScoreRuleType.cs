namespace SaveBite.Backend.Models.Enums;

// Match earning rules to the order's original payment method, not individual payment receipts.
public enum TrustScoreRuleType
{
    OnlineOrderCompleted = 1,
    PayAtShopOrderCompleted = 2,
    NoShow = 3
}
