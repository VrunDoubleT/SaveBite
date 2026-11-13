using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Audit;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;

namespace SaveBite.UnitTests.TrustScore;

public class AddNewPenaltyRuleTests
{
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly Mock<INotificationQueue> _notificationMock = new();
    private readonly DateTimeOffset _fixedNow = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    private readonly TrustConfigService _service;
    private readonly CreatePenaltyRuleRequestValidator _validator = new();

    public AddNewPenaltyRuleTests()
    {
        _service = new TrustConfigService(
            _repositoryMock.Object,
            _auditMock.Object,
            new FixedTimeProvider(_fixedNow),
            _loggerMock.Object,
            _notificationMock.Object);
    }

    [Fact]
    public async Task CreatePenaltyRuleAsync_WhenImmediateRuleWithoutOverlap_CreatesCurrentRuleAndAudit()
    {
        var adminId = Guid.NewGuid();
        var request = new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = 4,
            ScoreDeduction = 25,
            RequiredOnlineOrders = 3,
            RequiredOnlineOrderValue = 200000m,
            PermanentOfflineLock = false,
            IsActive = true,
            EffectiveFrom = null,
            EffectiveTo = null
        };

        _repositoryMock
            .Setup(r => r.FindOverlappingPenaltyRuleAsync(4, _fixedNow.UtcDateTime, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecoveryRequirementRule?)null);

        var result = await _service.CreatePenaltyRuleAsync(adminId, request);

        result.MinimumOccurrence.Should().Be(4);
        result.ScoreDeduction.Should().Be(25);
        result.IsCurrent.Should().BeTrue();
        result.EffectiveFrom.Should().Be(_fixedNow.UtcDateTime);

        _repositoryMock.Verify(r => r.AddPenaltyRule(It.Is<RecoveryRequirementRule>(rule =>
            rule.MinimumOccurrence == 4 &&
            rule.ScoreDeduction == 25 &&
            rule.CreatedByUserId == adminId)), Times.Once);
        _auditMock.Verify(a => a.Record(It.Is<AuditLog>(log => log.Action == AuditActionTypes.CreatePenaltyRule)), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePenaltyRuleAsync_WhenFutureScheduledRule_CreatesNonCurrentRule()
    {
        var adminId = Guid.NewGuid();
        var from = _fixedNow.UtcDateTime.AddDays(3);
        var to = _fixedNow.UtcDateTime.AddDays(10);

        var request = new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = 2,
            ScoreDeduction = 15,
            RequiredOnlineOrders = 2,
            RequiredOnlineOrderValue = 150000m,
            PermanentOfflineLock = false,
            IsActive = true,
            EffectiveFrom = from,
            EffectiveTo = to
        };

        _repositoryMock
            .Setup(r => r.FindOverlappingPenaltyRuleAsync(2, from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecoveryRequirementRule?)null);

        var result = await _service.CreatePenaltyRuleAsync(adminId, request);

        result.IsCurrent.Should().BeFalse();
        result.EffectiveFrom.Should().Be(from);
        result.EffectiveTo.Should().Be(to);
    }

    [Fact]
    public async Task CreatePenaltyRuleAsync_WhenOverlappingRuleExists_ThrowsConflict()
    {
        var existingConflict = new RecoveryRequirementRule
        {
            Id = Guid.NewGuid(),
            MinimumOccurrence = 1,
            EffectiveFrom = _fixedNow.UtcDateTime.AddDays(1),
            EffectiveTo = null
        };

        _repositoryMock
            .Setup(r => r.FindOverlappingPenaltyRuleAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime?>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConflict);

        var request = new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = 1,
            ScoreDeduction = 10,
            RequiredOnlineOrders = 1,
            RequiredOnlineOrderValue = 50000m,
            EffectiveFrom = _fixedNow.UtcDateTime.AddDays(2)
        };

        var act = () => _service.CreatePenaltyRuleAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Contain("overlaps with an existing rule for no-show #1");
        _repositoryMock.Verify(r => r.AddPenaltyRule(It.IsAny<RecoveryRequirementRule>()), Times.Never);
    }

    [Fact]
    public async Task CreatePenaltyRuleAsync_WhenEffectiveFromInPast_ThrowsBadRequest()
    {
        var request = new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = 2,
            ScoreDeduction = 10,
            EffectiveFrom = _fixedNow.UtcDateTime.AddHours(-1)
        };

        var act = () => _service.CreatePenaltyRuleAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("The effective time cannot be in the past.");
    }

    [Fact]
    public async Task CreatePenaltyRuleAsync_WhenEffectiveToNotAfterEffectiveFrom_ThrowsBadRequest()
    {
        var from = _fixedNow.UtcDateTime.AddDays(2);
        var request = new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = 2,
            ScoreDeduction = 10,
            EffectiveFrom = from,
            EffectiveTo = from
        };

        var act = () => _service.CreatePenaltyRuleAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("The 'Until' date/time must be strictly later than the 'From' date/time.");
    }

    [Fact]
    public void CreatePenaltyRuleRequestValidator_ValidatesRangesAndDateOrder()
    {
        _validator.Validate(new CreatePenaltyRuleRequest { MinimumOccurrence = 0, ScoreDeduction = 10 }).IsValid.Should().BeFalse();
        _validator.Validate(new CreatePenaltyRuleRequest { MinimumOccurrence = 1, ScoreDeduction = 0 }).IsValid.Should().BeFalse();
        _validator.Validate(new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = 1,
            ScoreDeduction = 10,
            EffectiveFrom = _fixedNow.UtcDateTime.AddDays(2),
            EffectiveTo = _fixedNow.UtcDateTime.AddDays(1)
        }).IsValid.Should().BeFalse();
        _validator.Validate(new CreatePenaltyRuleRequest
        {
            MinimumOccurrence = 3,
            ScoreDeduction = 20,
            RequiredOnlineOrders = 2,
            RequiredOnlineOrderValue = 100000m
        }).IsValid.Should().BeTrue();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
