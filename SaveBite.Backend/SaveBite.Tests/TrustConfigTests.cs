using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

public class TrustConfigTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);
    private static RecoveryRequirementRule Rule(int number, int start, int? end = null) => new()
    {
        Id = Guid.NewGuid(), MinimumOccurrence = number, ScoreDeduction = 10, RequiredOnlineOrders = 1,
        EffectiveFrom = Now.AddDays(start), EffectiveTo = end.HasValue ? Now.AddDays(end.Value) : null
    };
    private static CreatePenaltyRuleRequest Create(int start) => new() { MinimumOccurrence = 1, ScoreDeduction = 20, RequiredOnlineOrders = 2, EffectiveFrom = Now.AddDays(start) };

    [Fact] public async Task CreatingSuccessorAutomaticallyClosesOpenEndedPredecessorAndSchedulesBoth()
    {
        var fixture = new Fixture(); var original = Rule(1, -10); fixture.Proxy.Rules.Add(original);
        var result = await fixture.Service.CreatePenaltyRuleAsync(Guid.NewGuid(), Create(5));
        Assert.Equal(Now.AddDays(5), original.EffectiveTo);
        Assert.Null(result.EffectiveTo);
        Assert.Contains(original.Id, fixture.Scheduler.Synced);
        Assert.Contains(result.Id, fixture.Scheduler.Synced);
        Assert.Equal(2, fixture.Audit.Entries.Count);
    }

    [Fact] public async Task InsertingVersionBeforeExistingFutureRuleDefaultsEndToNextStart()
    {
        var fixture = new Fixture(); fixture.Proxy.Rules.AddRange([Rule(1, -5, 2), Rule(1, 10)]);
        var result = await fixture.Service.CreatePenaltyRuleAsync(Guid.NewGuid(), Create(5));
        Assert.Equal(Now.AddDays(10), result.EffectiveTo);
    }

    [Fact] public async Task CreatingGapIsAllowedWhenNoHigherThresholdApplies()
    {
        var fixture = new Fixture(); fixture.Proxy.Rules.Add(Rule(1, -5, 2));
        var result = await fixture.Service.CreatePenaltyRuleAsync(Guid.NewGuid(), Create(5));
        Assert.Equal(Now.AddDays(5), result.EffectiveFrom);
    }

    [Fact] public async Task CreatingGapCannotLeaveScheduledHigherPenaltyWithoutCoverage()
    {
        var fixture = new Fixture(); fixture.Proxy.Rules.AddRange([Rule(1, -5, 2), Rule(2, 1, 8)]);
        await Assert.ThrowsAsync<AppException>(() => fixture.Service.CreatePenaltyRuleAsync(Guid.NewGuid(), Create(5)));
        Assert.Empty(fixture.Scheduler.Synced);
    }

    [Fact] public async Task DeletingFutureLowerRuleCannotBreakHigherPenaltyCoverage()
    {
        var fixture = new Fixture(); var future = Rule(1, 2); fixture.Proxy.Rules.AddRange([Rule(1, -5, 2), future, Rule(2, 3, 8)]);
        await Assert.ThrowsAsync<AppException>(() => fixture.Service.DeletePenaltyRuleAsync(Guid.NewGuid(), future.Id));
        Assert.Contains(future, fixture.Proxy.Rules);
    }

    [Fact] public async Task EditingEarningStatusDoesNotNeedPointChangeOrCreateNewVersion()
    {
        var fixture = new Fixture(); var earning = new TrustScoreRule { Id = Guid.NewGuid(), RuleType = TrustScoreRuleType.OnlineOrderCompleted, ScoreDelta = 3, IsActive = true, CreatedAt = Now.AddDays(-5) };
        fixture.Proxy.Earning = earning;
        var result = await fixture.Service.UpdateEarningRuleAsync(Guid.NewGuid(), earning.Id, new() { ScoreDelta = 3, IsActive = false });
        Assert.Equal(earning.Id, result.Id);
        Assert.False(result.IsActive);
        Assert.Equal(earning.CreatedAt, result.CreatedAt);
        Assert.Single(fixture.Audit.Entries);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(2)]
    public async Task StartedPenaltyCannotMoveItsEffectiveFrom(int newStart)
    {
        var fixture = new Fixture();
        var rule = Rule(1, -5);
        fixture.Proxy.Rules.Add(rule);
        await Assert.ThrowsAsync<AppException>(() => fixture.Service.UpdatePenaltyRuleAsync(
            Guid.NewGuid(), rule.Id, new() { EffectiveFrom = Now.AddDays(newStart) }));
        Assert.Equal(Now.AddDays(-5), rule.EffectiveFrom);
        Assert.Empty(fixture.Scheduler.Synced);
    }

    private sealed class Fixture
    {
        public ConfigProxy Proxy { get; }
        public FakeScheduler Scheduler { get; } = new();
        public AuditWriter Audit { get; } = new();
        public TrustConfigService Service { get; }
        public Fixture()
        {
            var repository = DispatchProxy.Create<ITrustConfigRepository, ConfigProxy>();
            Proxy = (ConfigProxy)(object)repository;
            Service = new(repository, Audit, new FixedClock(), NullLogger<TrustConfigService>.Instance, null!, Scheduler);
        }
    }
    public class ConfigProxy : DispatchProxy
    {
        public List<RecoveryRequirementRule> Rules { get; } = [];
        public TrustScoreRule? Earning { get; set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            args ??= [];
            switch (method!.Name)
            {
                case "ExecuteInTransactionAsync": return ((Delegate)args[0]!).DynamicInvoke((CancellationToken)args[2]!);
                case "LockPenaltyRulesAsync": return Task.FromResult<IReadOnlyList<RecoveryRequirementRule>>(Rules);
                case "AddPenaltyRule": Rules.Add((RecoveryRequirementRule)args[0]!); return null;
                case "RemovePenaltyRule": Rules.Remove((RecoveryRequirementRule)args[0]!); return null;
                case "IsPenaltyRuleReferencedAsync": return Task.FromResult(false);
                case "SaveChangesAsync": return Task.CompletedTask;
                case "GetScoreRuleForUpdateAsync": return Task.FromResult(Earning);
                default: throw new NotSupportedException(method.Name);
            }
        }
    }
    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }
    private sealed class FakeScheduler : IPenaltyRuleScheduler
    {
        public List<Guid> Synced { get; } = [];
        public Task SyncAsync(Guid id, CancellationToken ct) { Synced.Add(id); return Task.CompletedTask; }
        public void Cancel(string? startId, string? endId) { }
    }
    private sealed class AuditWriter : IAdminAuditWriter
    {
        public List<AuditLog> Entries { get; } = [];
        public AuditLog Record(AuditLog entry) { Entries.Add(entry); return entry; }
    }
}
