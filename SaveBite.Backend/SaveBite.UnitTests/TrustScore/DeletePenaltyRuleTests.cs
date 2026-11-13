using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Audit;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.UnitTests.TrustScore;

public class DeletePenaltyRuleTests
{
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly Mock<INotificationQueue> _notificationMock = new();
    private readonly DateTimeOffset _fixedNow = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    private readonly TrustConfigService _service;

    public DeletePenaltyRuleTests()
    {
        _service = new TrustConfigService(
            _repositoryMock.Object,
            _auditMock.Object,
            new FixedTimeProvider(_fixedNow),
            _loggerMock.Object,
            _notificationMock.Object);
    }

    [Fact]
    public async Task DeletePenaltyRuleAsync_WhenFutureUnreferencedRule_RemovesWritesAuditAndSaves()
    {
        var adminId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var rule = new RecoveryRequirementRule
        {
            Id = ruleId,
            MinimumOccurrence = 3,
            ScoreDeduction = 20,
            RequiredOnlineOrders = 2,
            RequiredOnlineOrderValue = 100000m,
            EffectiveFrom = _fixedNow.UtcDateTime.AddDays(5)
        };

        _repositoryMock.Setup(r => r.GetPenaltyRuleForUpdateAsync(ruleId, It.IsAny<CancellationToken>())).ReturnsAsync(rule);
        _repositoryMock.Setup(r => r.IsPenaltyRuleReferencedAsync(ruleId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _service.DeletePenaltyRuleAsync(adminId, ruleId);

        _repositoryMock.Verify(r => r.RemovePenaltyRule(rule), Times.Once);
        _auditMock.Verify(a => a.Record(It.Is<AuditLog>(log =>
            log.Action == AuditActionTypes.DeletePenaltyRule &&
            log.TargetId == ruleId &&
            log.ActorUserId == adminId)), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePenaltyRuleAsync_WhenRuleNotFound_ThrowsNotFound()
    {
        var ruleId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetPenaltyRuleForUpdateAsync(ruleId, It.IsAny<CancellationToken>())).ReturnsAsync((RecoveryRequirementRule?)null);

        var act = () => _service.DeletePenaltyRuleAsync(Guid.NewGuid(), ruleId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Penalty rule not found.");
    }

    [Fact]
    public async Task DeletePenaltyRuleAsync_WhenRuleAlreadyInEffect_ThrowsBadRequest()
    {
        var ruleId = Guid.NewGuid();
        var activeRule = new RecoveryRequirementRule
        {
            Id = ruleId,
            MinimumOccurrence = 2,
            ScoreDeduction = 10,
            EffectiveFrom = _fixedNow.UtcDateTime
        };

        _repositoryMock.Setup(r => r.GetPenaltyRuleForUpdateAsync(ruleId, It.IsAny<CancellationToken>())).ReturnsAsync(activeRule);

        var act = () => _service.DeletePenaltyRuleAsync(Guid.NewGuid(), ruleId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Only future scheduled penalty rules that have not taken effect yet can be deleted.");
        _repositoryMock.Verify(r => r.RemovePenaltyRule(It.IsAny<RecoveryRequirementRule>()), Times.Never);
    }

    [Fact]
    public async Task DeletePenaltyRuleAsync_WhenRuleReferencedByCustomerPenalties_ThrowsConflict()
    {
        var ruleId = Guid.NewGuid();
        var rule = new RecoveryRequirementRule
        {
            Id = ruleId,
            MinimumOccurrence = 2,
            ScoreDeduction = 10,
            EffectiveFrom = _fixedNow.UtcDateTime.AddDays(2)
        };

        _repositoryMock.Setup(r => r.GetPenaltyRuleForUpdateAsync(ruleId, It.IsAny<CancellationToken>())).ReturnsAsync(rule);
        _repositoryMock.Setup(r => r.IsPenaltyRuleReferencedAsync(ruleId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _service.DeletePenaltyRuleAsync(Guid.NewGuid(), ruleId);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Cannot delete a penalty rule that has already been applied to customer penalties.");
        _repositoryMock.Verify(r => r.RemovePenaltyRule(It.IsAny<RecoveryRequirementRule>()), Times.Never);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
