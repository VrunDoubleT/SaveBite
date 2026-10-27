using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Helpers;

// Preserve the original fee and waive its remaining charge through a credit adjustment.
public static class RefundFeePolicy
{
    public static PlatformFeeAdjustment? Waive(Order order, Refund refund, OrderPlatformFee? fee, DateTime now)
    {
        order.IsPlatformFeeExempt = true;
        if (fee == null) return null;
        var remaining = Math.Max(0, fee.FeeAmount + fee.Adjustments.Sum(x => x.Amount));
        if (remaining == 0) return null;
        return new PlatformFeeAdjustment { Id = Guid.NewGuid(), ShopId = order.ShopId, OrderFeeId = fee.Id,
            RefundId = refund.Id, Amount = -remaining, Reason = "Full refund fee waiver", CreatedAt = now };
    }
}
