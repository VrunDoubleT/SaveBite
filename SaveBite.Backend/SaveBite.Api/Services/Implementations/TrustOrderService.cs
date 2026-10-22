using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.Services.Implementations;

public sealed class TrustOrderService : ITrustOrderService
{
    private readonly ITrustOrderRepository _repository;
    private readonly TimeProvider _clock;
    public TrustOrderService(ITrustOrderRepository repository, TimeProvider clock)
    { _repository = repository; _clock = clock; }

    // Update score, progress, penalty snapshots, recovery and level history in one customer-scoped transaction.
    public Task HandleAsync(Guid orderId, bool completed, CancellationToken ct = default)
        => _repository.ExecuteAsync(orderId, async order =>
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
            if (completed)
            {
                progress.CompletedOrdersCount = checked(progress.CompletedOrdersCount + 1);
                progress.AccumulatedOrderValue += order.TotalAmount;
                progress.UpdatedAt = now;
                var type = order.PaymentMethod == PaymentMethod.OnlinePayment
                    ? TrustScoreRuleType.OnlineOrderCompleted : TrustScoreRuleType.PayAtShopOrderCompleted;
                var rule = await _repository.EarningAsync(type, at, ct);
                if (rule != null) ChangeScore(score, order, rule.ScoreDelta, rule.Id, null, TrustScoreEventType.OrderCompleted, at);
            }
            else
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
                    score.IsOfflineLockedPermanently |= rule.PermanentOfflineLock;
                    ChangeScore(score, order, -penalty.ScoreDeducted, null, penalty.Id, TrustScoreEventType.NoShow, at);
                }
            }
            // Reconcile eligible historical completions too, so delayed NoShow events do not lose recovery progress.
            var recovering = await _repository.RecoveringAsync(order.CustomerId, ct);
            if (penalty != null) recovering.Add(penalty);
            var candidates = await _repository.RecoveryCandidatesAsync(order.CustomerId, ct);
            foreach (var p in recovering.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            {
                if (p.PermanentOfflineLock) continue;
                foreach (var candidate in candidates.Where(x => x.CompletedAt >= p.CreatedAt).ToList())
                {
                    if (Recovered(p)) break;
                    _repository.Add(new NoShowPenaltyOrderHistory { Id = Guid.NewGuid(), NoShowPenaltyId = p.Id,
                        OrderId = candidate.Id, OrderValue = candidate.TotalAmount, CreatedAt = candidate.CompletedAt!.Value });
                    p.CompletedOnlineOrders = checked(p.CompletedOnlineOrders + 1);
                    p.CompletedOnlineOrderValue += candidate.TotalAmount;
                    candidates.Remove(candidate);
                }
                if (Recovered(p)) { p.Status = NoShowPenaltyStatus.Recovered; p.RecoveredAt = now; }
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
            _repository.Add(new TrustOrderProcessing { OrderId = order.Id, ProcessedAt = now });
        }, ct);

    // Both independent recovery requirements must be met; a zero requirement is already satisfied.
    private static bool Recovered(NoShowPenalty p)
        => p.CompletedOnlineOrders >= p.RequiredOnlineOrders && p.CompletedOnlineOrderValue >= p.RequiredOnlineOrderValue;

    // Save the actual clamped delta and immutable source order/rule links for audit and support.
    private void ChangeScore(UserTrustScore score, Order order, int delta, Guid? ruleId, Guid? penaltyId, TrustScoreEventType type, DateTime at)
    {
        var before = score.CurrentScore;
        score.CurrentScore = checked((int)Math.Max(0L, (long)before + delta));
        _repository.Add(new TrustScoreHistory { Id = Guid.NewGuid(), UserId = score.UserId, OrderId = order.Id,
            RuleId = ruleId, NoShowPenaltyId = penaltyId, EventType = type, ScoreBefore = before,
            ScoreDelta = score.CurrentScore - before, ScoreAfter = score.CurrentScore,
            Reason = type == TrustScoreEventType.NoShow ? "NoShow penalty applied." : $"Completed {order.PaymentMethod} order.", CreatedAt = at });
    }
    // Link level transitions to their source order and penalty without resetting earned totals.
    private void AddLevelHistory(UserTrustScore score, Guid? previous, Guid next, TrustLevelChangeReason reason, Order order, Guid? penaltyId, DateTime at)
        => _repository.Add(new UserTrustLevelHistory { Id = Guid.NewGuid(), UserId = score.UserId,
            PreviousTrustLevelId = previous, NewTrustLevelId = next, Reason = reason, OrderId = order.Id,
            NoShowPenaltyId = penaltyId, CreatedAt = at });
}
