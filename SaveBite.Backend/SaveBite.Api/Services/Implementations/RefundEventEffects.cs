using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

public sealed class RefundEventEffects(AppDbContext db, TimeProvider clock, ITrustNotificationService notifications)
{
    public async Task ApplyFeeAsync(Guid refundId, CancellationToken ct)
    {
        var orderId = await db.Refunds.AsNoTracking().Where(f => f.Id == refundId).Select(f => f.OrderId).SingleAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockOrderAsync(db, orderId, ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", ct);
        var order = await db.Orders.Include(o => o.Refunds).SingleAsync(o => o.Id == orderId, ct);
        await ReconcileFeesAsync(db, order, clock.GetUtcNow().UtcDateTime, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public static async Task LockOrderAsync(AppDbContext db, Guid orderId, CancellationToken ct)
    {
        var userId = await db.Orders.AsNoTracking().Where(o => o.Id == orderId).Select(o => o.CustomerId).SingleAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM users WHERE id = {userId} FOR UPDATE", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM orders WHERE id = {orderId} FOR UPDATE", ct);
        db.ChangeTracker.Clear();
    }

    // Reconcile against absolute targets, so completion/refund messages can arrive in either order.
    // FeeAmount remains the original accrual; every reduction is an auditable adjustment.
    public static async Task ReconcileFeesAsync(AppDbContext db, Order order, DateTime now, CancellationToken ct)
    {
        var fee = await db.OrderPlatformFees.Include(f => f.Adjustments).Include(f => f.Statement)
            .SingleOrDefaultAsync(f => f.OrderId == order.Id, ct);
        if (fee == null) return; // A later completion accrual reconciles all already-transferred refunds atomically.
        decimal refunded = 0;
        foreach (var refund in order.Refunds.Where(f => f.TransferredAmount > 0).OrderBy(f => f.TransferredAt).ThenBy(f => f.Id))
        {
            refunded += refund.TransferredAmount;
            var target = Math.Round(Math.Max(0, order.TotalAmount - refunded) * fee.FeeRate, 2, MidpointRounding.AwayFromZero);
            var effective = Math.Max(0, fee.FeeAmount + fee.Adjustments.Sum(a => a.Amount));
            var credit = Math.Max(0, effective - target);
            if (credit > 0)
            {
                var adjustment = new PlatformFeeAdjustment { Id = Guid.NewGuid(), ShopId = order.ShopId,
                    OrderFeeId = fee.Id, RefundId = refund.Id, Amount = -credit,
                    Reason = "Transferred refund: reconcile retained order value", CreatedAt = now };
                if (fee.Statement != null && fee.Statement.Status != PlatformFeeStatementStatus.Paid)
                {
                    adjustment.StatementId = fee.Statement.Id;
                    var previous = fee.Statement.TotalFeeAmount;
                    fee.Statement.TotalFeeAmount = Math.Max(0, previous - credit);
                    fee.Statement.UpdatedAt = now;
                    if (fee.Statement.TotalFeeAmount == 0)
                    { fee.Statement.Status = PlatformFeeStatementStatus.Paid; fee.Statement.PaidAt = now; fee.Statement.DueAt = null; }
                    db.Add(new PlatformFeeStatementHistory { Id = Guid.NewGuid(), StatementId = fee.Statement.Id,
                        PreviousAmount = previous, ChangeAmount = fee.Statement.TotalFeeAmount - previous, NewAmount = fee.Statement.TotalFeeAmount,
                        ChangeType = refunded >= order.TotalAmount ? StatementChangeType.FullRefundDeduction : StatementChangeType.PartialRefundDeduction,
                        RelatedRefundId = refund.Id, RelatedOrderId = order.Id, Reason = adjustment.Reason, CreatedAt = now });
                }
                db.Add(adjustment); // Explicit Added state: the non-empty UUID must never be treated as an existing row.
            }
            refund.FeeAdjustedAt = now;
        }
        order.IsPlatformFeeExempt = order.TotalAmount <= refunded;
    }

    public async Task ApplyTrustAsync(Guid refundId, CancellationToken ct)
    {
        var orderId = await db.Refunds.AsNoTracking().Where(f => f.Id == refundId).Select(f => f.OrderId).SingleAsync(ct);
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await LockOrderAsync(db, orderId, ct);
            var order = await db.Orders.Include(o => o.Refunds).SingleAsync(o => o.Id == orderId, ct);
            var refund = order.Refunds.Single(f => f.Id == refundId);
            if (refund.TransferredAmount <= 0) return;
            var now = clock.GetUtcNow().UtcDateTime;
            var receipt = await db.Set<TrustOrderProcessing>().SingleOrDefaultAsync(p => p.OrderId == orderId, ct);
            if (order.Status == OrderStatus.Completed)
            {
                if (receipt == null && await db.TrustScoreHistories.AnyAsync(h => h.OrderId == orderId && h.EventType == TrustScoreEventType.OrderCompleted, ct))
                {
                    receipt = new TrustOrderProcessing { OrderId = orderId, ProcessedAt = now,
                        CreditedOrderValue = Math.Max(0, order.TotalAmount - order.Refunds.Where(f => f.Id != refundId && f.AccumulatedValueReclaimedAt != null).Sum(f => f.TransferredAmount)) };
                    db.Add(receipt);
                }
                if (receipt != null)
                {
                    var target = Math.Max(0, order.TotalAmount - order.RefundedAmount);
                    var reclaim = Math.Max(0, receipt.CreditedOrderValue - target);
                    if (reclaim > 0)
                    {
                        var progress = await db.UserLevelProgresses.SingleAsync(p => p.UserId == order.CustomerId, ct);
                        var score = await db.UserTrustScores.SingleAsync(p => p.UserId == order.CustomerId, ct);
                        var levels = await db.TrustLevels.Include(l => l.Requirement).Where(l => l.IsActive).OrderBy(l => l.Rank).ToListAsync(ct);
                        if (levels.Count == 0) throw new InvalidOperationException("Active trust levels are required.");
                        progress.AccumulatedOrderValue = Math.Max(0, progress.AccumulatedOrderValue - reclaim);
                        progress.UpdatedAt = now;
                        receipt.CreditedOrderValue -= reclaim;
                        var level = levels.Where(l => l.Requirement != null && l.Requirement.MinScore <= score.CurrentScore
                            && l.Requirement.MinCompletedOrders <= progress.CompletedOrdersCount
                            && l.Requirement.MinTotalOrderValue <= progress.AccumulatedOrderValue).OrderByDescending(l => l.Rank).FirstOrDefault() ?? levels[0];
                        if (level.Id != score.TrustLevelId)
                        {
                            db.Add(new UserTrustLevelHistory { Id = Guid.NewGuid(), UserId = score.UserId,
                                PreviousTrustLevelId = score.TrustLevelId, NewTrustLevelId = level.Id,
                                Reason = TrustLevelChangeReason.RefundTransferred, OrderId = order.Id, CreatedAt = now });
                            score.TrustLevelId = level.Id; score.UpdatedAt = now;
                        }
                    }
                }
                // If completion has not been consumed, its consumer will credit the already-refunded net amount.
            }
            refund.AccumulatedValueReclaimedAt = now;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        await notifications.PublishForOrderAsync(orderId, ct);
    }
}
