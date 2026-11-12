using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

public class TrustPolicyTests
{
    private static readonly DateTime Day = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);
    private static RecoveryRequirementRule Rule(int occurrence, int from, int? until = null) => new()
    {
        Id = Guid.NewGuid(), MinimumOccurrence = occurrence, EffectiveFrom = Day.AddDays(from),
        EffectiveTo = until.HasValue ? Day.AddDays(until.Value) : null, IsActive = true,
        ScoreDeduction = occurrence * 10, RequiredOnlineOrders = occurrence + 1, RequiredOnlineOrderValue = occurrence * 50000
    };

    [Fact] public void ScheduleAllowsGapsWithoutHigherPenalties() =>
        PenaltyScheduleRules.Validate([Rule(1, 0, 2), Rule(1, 5)], Day);

    [Fact] public void ScheduleRejectsGapInsideHigherPenaltyPeriod() =>
        Assert.Throws<AppException>(() => PenaltyScheduleRules.Validate([Rule(1, 0, 2), Rule(1, 5), Rule(2, 1, 6)], Day));

    [Fact] public void ScheduleAllowsContiguousLowerVersionsCoveringHigherPenalty() =>
        PenaltyScheduleRules.Validate([Rule(1, 0, 2), Rule(1, 2), Rule(2, 1, 6)], Day);

    [Fact] public void ScheduleRejectsMissingLowerThreshold() =>
        Assert.Throws<AppException>(() => PenaltyScheduleRules.Validate([Rule(1, 0), Rule(3, 1)], Day));

    [Fact] public void ScheduleRejectsOpenEndedHigherWithFiniteLowerCoverage() =>
        Assert.Throws<AppException>(() => PenaltyScheduleRules.Validate([Rule(1, 0, 5), Rule(2, 2)], Day));

    [Fact] public void ScheduleIgnoresDisabledHigherRule()
    {
        var higher = Rule(2, 1, 6); higher.IsActive = false;
        PenaltyScheduleRules.Validate([Rule(1, 0, 2), Rule(1, 5), higher], Day);
    }

    [Fact] public void ScheduleRejectsOverlappingVersions() =>
        Assert.Throws<AppException>(() => PenaltyScheduleRules.Validate([Rule(1, 0, 4), Rule(1, 3)], Day));

    [Fact] public void HistoricalGapsDoNotBlockFuturePolicyEdits() =>
        PenaltyScheduleRules.Validate([Rule(1, -5, -3), Rule(2, -4, -2), Rule(1, 0)], Day);

    [Fact] public async Task NewViolationReplacesPartialRecoveryAndRestartsAtZero()
    {
        var repo = new MemoryTrustRepository(); var service = Service(repo);
        await Handle(service, repo, false, 0);
        await Handle(service, repo, true, 1);
        var previous = Assert.Single(repo.Penalties);
        Assert.Equal(1, previous.CompletedOnlineOrders);
        await Handle(service, repo, false, 2);
        Assert.Equal(NoShowPenaltyStatus.Superseded, previous.Status);
        Assert.Equal(Day.AddHours(2), previous.SupersededAt);
        Assert.Equal(1, previous.CompletedOnlineOrders);
        var current = Assert.Single(repo.Penalties.Where(p => p.Status == NoShowPenaltyStatus.Recovering));
        Assert.Equal(2, current.OccurrenceNumber);
        Assert.Equal(0, current.CompletedOnlineOrders);
        Assert.Equal(0, current.CompletedOnlineOrderValue);
        Assert.Equal(70, repo.Score.CurrentScore);
        Assert.Equal(5, repo.Progress.CompletedOrdersCount);
        Assert.Equal(100000, repo.Progress.AccumulatedOrderValue);
    }

    [Fact] public async Task ViolationsAboveHighestThresholdReuseItsRequirementsWithoutStacking()
    {
        var repo = new MemoryTrustRepository(); var service = Service(repo);
        for (var i = 0; i < 4; i++) await Handle(service, repo, false, i);
        var current = Assert.Single(repo.Penalties.Where(p => p.Status == NoShowPenaltyStatus.Recovering));
        Assert.Equal(4, current.OccurrenceNumber);
        Assert.Equal(3, current.RequiredOnlineOrders); // highest configured threshold is 2
        Assert.Equal(3, repo.Penalties.Count(p => p.Status == NoShowPenaltyStatus.Superseded));
        Assert.Equal(30, repo.Score.CurrentScore);
    }

    [Fact] public async Task FinalRecoveryOrderDoesNotEarnPointsOrLevelProgressButNextOrderDoes()
    {
        var repo = new MemoryTrustRepository(); var service = Service(repo);
        await Handle(service, repo, false, 0);
        await Handle(service, repo, true, 1);
        await Handle(service, repo, true, 2);
        Assert.Equal(NoShowPenaltyStatus.Recovered, repo.Penalties[0].Status);
        Assert.Equal(90, repo.Score.CurrentScore);
        Assert.Equal(5, repo.Progress.CompletedOrdersCount);
        Assert.Equal(100000, repo.Progress.AccumulatedOrderValue);
        await Handle(service, repo, true, 3);
        Assert.Equal(93, repo.Score.CurrentScore);
        Assert.Equal(6, repo.Progress.CompletedOrdersCount);
        Assert.Equal(150000, repo.Progress.AccumulatedOrderValue);
    }

    [Fact] public async Task InactiveEarningRuleKeepsAccumulatingOrdersAndValue()
    {
        var repo = new MemoryTrustRepository { EarningActive = false };
        await Handle(Service(repo), repo, true, 0);
        Assert.Equal(100, repo.Score.CurrentScore);
        Assert.Equal(6, repo.Progress.CompletedOrdersCount);
        Assert.Equal(150000, repo.Progress.AccumulatedOrderValue);
    }

    [Fact] public async Task SameTimestampViolationsStillReplaceThePreviousPenalty()
    {
        var repo = new MemoryTrustRepository(); var service = Service(repo);
        await Handle(service, repo, false, 0);
        await Handle(service, repo, false, 0);
        Assert.Equal(NoShowPenaltyStatus.Superseded, repo.Penalties[0].Status);
        Assert.Equal(NoShowPenaltyStatus.Recovering, repo.Penalties[1].Status);
    }

    [Fact] public async Task DuplicateOrderEventCannotApplyPenaltyTwice()
    {
        var repo = new MemoryTrustRepository(); var service = Service(repo);
        var order = await Handle(service, repo, false, 0);
        await service.HandleAsync(order.Id, false);
        Assert.Single(repo.Penalties);
        Assert.Equal(90, repo.Score.CurrentScore);
    }

    [Fact] public async Task RecoveryOrdersAssignedToOldPenaltyCannotBeReusedForNewPenalty()
    {
        var repo = new MemoryTrustRepository(); var service = Service(repo);
        await Handle(service, repo, false, 0);
        await Handle(service, repo, true, 1);
        await Handle(service, repo, false, 2);
        await Handle(service, repo, true, 3);
        Assert.Equal(1, repo.Penalties[0].CompletedOnlineOrders);
        Assert.Equal(1, repo.Penalties[1].CompletedOnlineOrders);
    }

    [Fact] public async Task DelayedCompletionFromSupersededRecoveryDoesNotEarnProgress()
    {
        var repo = new MemoryTrustRepository(); var service = Service(repo);
        await Handle(service, repo, false, 0);
        await Handle(service, repo, false, 2);
        await Handle(service, repo, true, 1);
        Assert.Equal(70, repo.Score.CurrentScore);
        Assert.Equal(5, repo.Progress.CompletedOrdersCount);
        Assert.Equal(100000, repo.Progress.AccumulatedOrderValue);
        Assert.Equal(0, repo.Penalties[1].CompletedOnlineOrders);
    }

    [Fact] public void ArchiveFilteringUsesVietnamDatesAndMatchesIdentifiers()
    {
        var row = new AuditLog { Id = Guid.NewGuid(), TargetId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(),
            ActorType = AuditActorType.System, Action = "UPDATE_PENALTY_RULE", TargetType = "RecoveryRequirementRule",
            CreatedAt = Day.AddHours(-1), Reason = "Updated policy" };
        var query = new ArchiveLogQuery { FromDate = DateOnly.FromDateTime(Day), ToDate = DateOnly.FromDateTime(Day), Search = "penalty", CorrelationId = row.CorrelationId };
        Assert.True(query.Matches(row));
        query.CorrelationId = Guid.NewGuid(); Assert.False(query.Matches(row));
        query.CorrelationId = null; query.FromDate = DateOnly.FromDateTime(Day.AddDays(1)); Assert.False(query.Matches(row));
    }

    private static TrustOrderService Service(MemoryTrustRepository repo) => new(repo, TimeProvider.System, new SilentNotifications());
    private static async Task<Order> Handle(TrustOrderService service, MemoryTrustRepository repo, bool completed, int hour)
    {
        var order = new Order { Id = Guid.NewGuid(), CustomerId = repo.Score.UserId, OrderCode = Guid.NewGuid().ToString(),
            TotalAmount = 50000, PaymentMethod = PaymentMethod.OnlinePayment, Status = completed ? OrderStatus.Completed : OrderStatus.NoShow,
            CompletedAt = completed ? Day.AddHours(hour) : null };
        if (!completed) order.StatusHistories.Add(new OrderStatusHistory { ToStatus = OrderStatus.NoShow, CreatedAt = Day.AddHours(hour) });
        repo.Orders.Add(order);
        await service.HandleAsync(order.Id, completed);
        return order;
    }
    private sealed class SilentNotifications : ITrustNotificationService
    {
        public Task PublishForOrderAsync(Guid id, CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class MemoryTrustRepository : ITrustOrderRepository
    {
        public UserTrustScore Score { get; } = new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), CurrentScore = 100 };
        public UserLevelProgress Progress { get; } = new() { CompletedOrdersCount = 5, AccumulatedOrderValue = 100000 };
        public List<NoShowPenalty> Penalties { get; } = [];
        public List<Order> Orders { get; } = [];
        public bool EarningActive { get; set; } = true;
        private readonly HashSet<Guid> processed = [];
        private readonly HashSet<Guid> assigned = [];
        private readonly TrustLevel level = new() { Id = Guid.NewGuid(), Rank = 1, IsActive = true, Requirement = new TrustLevelRequirement() };
        public Task ExecuteAsync(Guid orderId, Func<Order, Task> handle, CancellationToken ct) => handle(Orders.Single(o => o.Id == orderId));
        public Task<bool> IsProcessedAsync(Guid id, CancellationToken ct) => Task.FromResult(processed.Contains(id));
        public Task<UserTrustScore?> ScoreAsync(Guid user, CancellationToken ct) { Score.TrustLevelId = level.Id; return Task.FromResult<UserTrustScore?>(Score); }
        public Task<UserLevelProgress?> ProgressAsync(Guid user, CancellationToken ct) => Task.FromResult<UserLevelProgress?>(Progress);
        public Task<List<TrustLevel>> LevelsAsync(CancellationToken ct) => Task.FromResult(new List<TrustLevel> { level });
        public Task<TrustScoreRule?> EarningAsync(TrustScoreRuleType type, DateTime at, CancellationToken ct) => Task.FromResult<TrustScoreRule?>(EarningActive ? new() { Id = Guid.NewGuid(), ScoreDelta = 3 } : null);
        public Task<int> OccurrencesAsync(Guid user, int year, int month, CancellationToken ct) => Task.FromResult(Penalties.Count(p => p.PenaltyYear == year && p.PenaltyMonth == month));
        public Task<RecoveryRequirementRule?> PenaltyRuleAsync(int occurrence, DateTime at, CancellationToken ct) => Task.FromResult<RecoveryRequirementRule?>(Rule(Math.Min(occurrence, 2), 0));
        public Task<List<NoShowPenalty>> PenaltiesAsync(Guid user, CancellationToken ct) => Task.FromResult(Penalties.ToList());
        public Task<List<Order>> RecoveryCandidatesAsync(Guid user, CancellationToken ct) => Task.FromResult(Orders.Where(o => o.Status == OrderStatus.Completed && o.PaymentMethod == PaymentMethod.OnlinePayment && !assigned.Contains(o.Id)).OrderBy(o => o.CompletedAt).ToList());
        public void Add<T>(T value) where T : class
        {
            if (value is NoShowPenalty penalty) Penalties.Add(penalty);
            if (value is TrustOrderProcessing receipt) processed.Add(receipt.OrderId);
            if (value is NoShowPenaltyOrderHistory history) { assigned.Add(history.OrderId); Penalties.Single(p => p.Id == history.NoShowPenaltyId).RecoveryOrders.Add(history); }
        }
    }
}
