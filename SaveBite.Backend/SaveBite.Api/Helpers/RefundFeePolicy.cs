using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Helpers;

// Preserve the original fee and credit only the charge attributable to refunded value.
public static class RefundFeePolicy
{
    public static PlatformFeeAdjustment? Waive(Order order, Refund refund, OrderPlatformFee? fee, DateTime now)
    {
        // Voluntary approval alone never waives fees; a successful refund must be recorded first.
        if (refund.RefundType == RefundType.CustomerRequest && refund.Status is not (RefundStatus.Transferred or RefundStatus.Completed))
            return null;
        var retained = refund.RefundType == RefundType.ShopCancellation ? 0 : Math.Max(0, order.TotalAmount - order.RefundedAmount);
        order.IsPlatformFeeExempt = retained == 0;
        if (fee == null) return null;
        var remaining = Math.Max(0, fee.FeeAmount + fee.Adjustments.Sum(x => x.Amount));
        var target = order.TotalAmount <= 0 ? 0 : decimal.Round(fee.FeeAmount * retained / order.TotalAmount, 2, MidpointRounding.AwayFromZero);
        var credit = Math.Max(0, remaining - target);
        if (credit == 0) return null;
        return new PlatformFeeAdjustment
        {
            Id = Guid.NewGuid(),
            ShopId = order.ShopId,
            OrderFeeId = fee.Id,
            RefundId = refund.Id,
            Amount = -credit,
            Reason = "Refund adjustment based on retained order value",
            CreatedAt = now
        };
    }
}
