using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Helpers;

public static class PlatformFeeCreditAllocator
{
    // Unassigned negative rows are the outstanding credit balance. Splitting keeps
    // the total credit unchanged and preserves the originating refund on each use.
    public static PlatformFeeAdjustment? Apply(PlatformFeeAdjustment credit, Guid statementId, decimal capacity)
    {
        if (credit.StatementId != null || credit.Amount >= 0 || capacity <= 0) return null;
        var applied = Math.Min(-credit.Amount, capacity);
        if (applied == -credit.Amount)
        {
            credit.StatementId = statementId;
            return credit;
        }

        credit.Amount += applied;
        return new PlatformFeeAdjustment
        {
            Id = Guid.NewGuid(), ShopId = credit.ShopId, OrderFeeId = credit.OrderFeeId,
            RefundId = credit.RefundId, StatementId = statementId, Amount = -applied,
            Reason = credit.Reason, CreatedAt = credit.CreatedAt
        };
    }
}
