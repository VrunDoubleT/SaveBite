using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

public sealed class TrustOrderService : ITrustOrderService
{
    private readonly ITrustOrderRepository _repository;
    private readonly TimeProvider _clock;
    private readonly ITrustNotificationService _notifications;
    private readonly BusinessEmailQueue? _emails;
    public TrustOrderService(ITrustOrderRepository repository, TimeProvider clock, ITrustNotificationService notifications, BusinessEmailQueue? emails = null)
    { _repository = repository; _clock = clock; _notifications = notifications; _emails = emails; }

    // Update score, progress, penalty snapshots, recovery and level history in one customer-scoped transaction.
    public async Task HandleAsync(Guid orderId, bool completed, CancellationToken ct = default)
    {
        await _repository.ExecuteAsync(orderId, async order =>
        {
            if (await _repository.IsProcessedAsync(order.Id, ct)) return;
            if (order.Status != (completed ? OrderStatus.Completed : OrderStatus.NoShow))
                throw new InvalidOperationException("The order has not reached the expected terminal status.");
            var at = completed ? order.CompletedAt ?? throw new InvalidOperationException("CompletedAt is required.")
                : order.StatusHistories.Where(x => x.ToStatus == OrderStatus.NoShow).Select(x => (DateTime?)x.CreatedAt).Min()
                    ?? throw new InvalidOperationException("NoShow history is required.");
            var now = _clock.GetUtcNow().UtcDateTime;
            var levels = await _repository.LevelsAsync(ct);
            if (levels.Count == 0) throw new InvalidOperationException("Active trust levels are not configured.");
            var score = await _repository.ScoreAsync(order.CustomerId, ct);
            if (score == null)
            {
                score = new UserTrustScore { Id = Guid.NewGuid(), UserId = order.CustomerId, TrustLevelId = levels[0].Id,
                    CurrentScore = 0, CreatedAt = now, UpdatedAt = now };
                _repository.Add(score);
                AddLevelHistory(score, null, levels[0].Id, TrustLevelChangeReason.InitialAssignment, order, null, now);
            }
            var progress = await _repository.ProgressAsync(order.CustomerId, ct);
            if (progress == null)
            {
                progress = new UserLevelProgress { Id = Guid.NewGuid(), UserId = order.CustomerId, CreatedAt = now, UpdatedAt = now };
                _repository.Add(progress);
            }
            NoShowPenalty? penalty = null;
            var creditedValue = 0m;
            var penalties = await _repository.PenaltiesAsync(order.CustomerId, ct);
            if (!completed)
            {
                var date = Helpers.OrderBusinessDate.FromUtc(at);
                var occurrence = await _repository.OccurrencesAsync(order.CustomerId, date.Year, date.Month, ct) + 1;
                var rule = await _repository.PenaltyRuleAsync(occurrence, at, ct);
                if (rule != null)
                {
                    penalty = new NoShowPenalty { Id = Guid.NewGuid(), UserId = order.CustomerId, OrderId = order.Id,
                        RecoveryRuleId = rule.Id, PenaltyYear = date.Year, PenaltyMonth = date.Month, OccurrenceNumber = occurrence,
                        ScoreDeducted = Math.Min(score.CurrentScore, rule.ScoreDeduction), RequiredOnlineOrders = rule.RequiredOnlineOrders,
                        RequiredOnlineOrderValue = rule.RequiredOnlineOrderValue, PermanentOfflineLock = rule.PermanentOfflineLock,
                        Status = NoShowPenaltyStatus.Recovering, CreatedAt = at };
                    _repository.Add(penalty);
                    if (rule.PermanentOfflineLock && !score.IsOfflineLockedPermanently && _emails != null)
                        await _emails.StageOfflineLockAsync(order.CustomerId, penalty.Id, ct);
                    score.IsOfflineLockedPermanently |= rule.PermanentOfflineLock;
                    ChangeScore(score, order, -penalty.ScoreDeducted, null, penalty.Id, TrustScoreEventType.NoShow, at,
                        progress.AccumulatedOrderValue, 0m);
                }
            }
            // Reconcile eligible historical completions too, so delayed NoShow events do not lose recovery progress.
            if (penalty != null) penalties.Add(penalty);
            // Normalize old/replayed penalties on every event, not just a new violation.
            // Only the latest penalty may own current recovery progress.
            var ordered = penalties.OrderBy(p => p.CreatedAt).ThenBy(p => p.OccurrenceNumber).ThenBy(p => p.Id).ToList();
            for (var i = 0; i < ordered.Count - 1; i++)
            {
                var previous = ordered[i];
                var next = ordered[i + 1];
                if (previous.Status == NoShowPenaltyStatus.Recovering || previous.RecoveredAt > next.CreatedAt || previous.SupersededAt > next.CreatedAt)
                {
                    previous.Status = NoShowPenaltyStatus.Superseded;
                    previous.SupersededAt = next.CreatedAt;
                    previous.RecoveredAt = null;
                }
            }
            var candidates = await _repository.RecoveryCandidatesAsync(order.CustomerId, ct);
            foreach (var p in ordered.TakeLast(1).Where(p => p.Status == NoShowPenaltyStatus.Recovering))
            {
                var recoveredAt = p.RecoveryOrders.Select(h => h.CreatedAt).DefaultIfEmpty(p.CreatedAt).Max();
                foreach (var candidate in candidates.Where(x => x.CompletedAt >= p.CreatedAt && (!completed || x.CompletedAt <= at) && !penalties.Any(next => next.CreatedAt > p.CreatedAt && x.CompletedAt >= next.CreatedAt)).ToList())
                {
                    if (Recovered(p)) break;
                    _repository.Add(new NoShowPenaltyOrderHistory { Id = Guid.NewGuid(), NoShowPenaltyId = p.Id,
                        OrderId = candidate.Id, OrderValue = candidate.TotalAmount, CreatedAt = candidate.CompletedAt!.Value });
                    p.CompletedOnlineOrders = checked(p.CompletedOnlineOrders + 1);
                    p.CompletedOnlineOrderValue += candidate.TotalAmount;
                    recoveredAt = candidate.CompletedAt!.Value > recoveredAt ? candidate.CompletedAt.Value : recoveredAt;
                    candidates.Remove(candidate);
                }
                if (Recovered(p)) { p.Status = NoShowPenaltyStatus.Recovered; p.RecoveredAt = recoveredAt; }
            }
            // Recovery consumes the whole order, including the one that satisfies the final requirement.
            // Event timestamps also keep delayed completion deliveries inside the original recovery interval.
            var recoveringAtCompletion = penalties.Any(p => p.CreatedAt <= at
                && (p.Status == NoShowPenaltyStatus.Recovering || p.RecoveredAt >= at || p.SupersededAt >= at));
            if (completed && !recoveringAtCompletion)
            {
                progress.CompletedOrdersCount = checked(progress.CompletedOrdersCount + 1);
                // A transfer can precede delayed completion delivery; refunded orders never earn accumulated value.
                creditedValue = Math.Max(0, order.TotalAmount - order.RefundedAmount);
                var valueBefore = progress.AccumulatedOrderValue;
                progress.AccumulatedOrderValue += creditedValue;
                progress.UpdatedAt = now;
                var type = order.PaymentMethod == PaymentMethod.OnlinePayment
                    ? TrustScoreRuleType.OnlineOrderCompleted : TrustScoreRuleType.PayAtShopOrderCompleted;
                var rule = await _repository.EarningAsync(type, at, ct);
                // Always write one ledger row: even without an active score rule the accumulated money still changed.
                ChangeScore(score, order, rule?.ScoreDelta ?? 0, rule?.Id, null, TrustScoreEventType.OrderCompleted, at,
                    valueBefore, creditedValue);
            }
            // Retain accumulated totals and choose the highest active level meeting all three requirements.
            // A NoShow with no applicable penalty rule has no trust/level consequence.
            if (completed || penalty != null)
            {
                var level = levels.Where(x => x.Requirement != null && score.CurrentScore >= x.Requirement.MinScore
                    && progress.CompletedOrdersCount >= x.Requirement.MinCompletedOrders
                    && progress.AccumulatedOrderValue >= x.Requirement.MinTotalOrderValue).OrderByDescending(x => x.Rank).FirstOrDefault() ?? levels[0];
                if (score.TrustLevelId != level.Id)
                {
                    AddLevelHistory(score, score.TrustLevelId, level.Id,
                        completed ? TrustLevelChangeReason.RequirementsMet : TrustLevelChangeReason.NoShowPenalty, order, penalty?.Id, at);
                    score.TrustLevelId = level.Id;
                }
            }
            score.UpdatedAt = now;
            _repository.Add(new TrustOrderProcessing { OrderId = order.Id, ProcessedAt = now, CreditedOrderValue = creditedValue });
        }, ct);

    }

    // Both independent recovery requirements must be met; a zero requirement is already satisfied.
    private static bool Recovered(NoShowPenalty p)
        => p.CompletedOnlineOrders >= p.RequiredOnlineOrders && p.CompletedOnlineOrderValue >= p.RequiredOnlineOrderValue;

    // Save the actual clamped delta and immutable source order/rule links for audit and support.
    // Save the actual clamped delta and immutable source order/rule links for audit and support.
    private void ChangeScore(UserTrustScore score, Order order, int delta, Guid? ruleId, Guid? penaltyId, TrustScoreEventType type, DateTime at,
        decimal valueBefore, decimal valueDelta, NoShowPenalty? penalty = null)
    {
        var before = score.CurrentScore;
        score.CurrentScore = checked((int)Math.Max(0L, (long)before + delta));
        var reason = type == TrustScoreEventType.NoShow
            ? (penalty != null
                ? $"No-show #{penalty.OccurrenceNumber} in {penalty.PenaltyMonth:D2}/{penalty.PenaltyYear} on order {order.OrderCode}."
                : $"No-show penalty on order {order.OrderCode}.")
            : $"Completed {order.PaymentMethod} order {order.OrderCode}.";

        _repository.Add(new TrustProgressHistory { Id = Guid.NewGuid(), UserId = score.UserId, OrderId = order.Id,
            RuleId = ruleId, NoShowPenaltyId = penaltyId, EventType = type, ScoreBefore = before,
            ScoreDelta = score.CurrentScore - before, ScoreAfter = score.CurrentScore,
            ValueBefore = valueBefore, ValueDelta = valueDelta, ValueAfter = valueBefore + valueDelta,
            Reason = reason, CreatedAt = at });
    }
    // Link level transitions to their source order and penalty without resetting earned totals.
    private void AddLevelHistory(UserTrustScore score, Guid? previous, Guid next, TrustLevelChangeReason reason, Order order, Guid? penaltyId, DateTime at)
        => _repository.Add(new UserTrustLevelHistory { Id = Guid.NewGuid(), UserId = score.UserId,
            PreviousTrustLevelId = previous, NewTrustLevelId = next, Reason = reason, OrderId = order.Id,
            NoShowPenaltyId = penaltyId, CreatedAt = at });
}
