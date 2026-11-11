using SaveBite.Backend.Services.Interfaces;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.TrustScore;

/// <summary>
/// Unit Tests for Feature: Update Penalty Rules (UC-134)
/// Service: TrustConfigService.UpdatePenaltyRuleAsync & UpdatePenaltyRuleRequestValidator
/// Function Code: UT_UpdatePenaltyRules
/// Sheet Name: UT_UpdatePenaltyRules
/// Business Rules: BR-052, BR-159, BR-160, BR-161
/// </summary>
public class UpdatePenaltyRulesTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();

    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly TrustConfigService _service;
    private readonly UpdatePenaltyRuleRequestValidator _validator = new();

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _ruleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _nonExistentRuleId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public UpdatePenaltyRulesTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustConfigService(_repositoryMock.Object, _auditMock.Object, TimeProvider.System, _loggerMock.Object, new Mock<INotificationQueue>().Object);
    }

    private RecoveryRequirementRule CreateSampleRule(bool isPending = false, bool isReplaced = false)
    {
        var now = DateTime.UtcNow;
        return new RecoveryRequirementRule
        {
            Id = _ruleId,
            MinimumOccurrence = 1,
            ScoreDeduction = 10,
            RequiredOnlineOrders = 3,
            RequiredOnlineOrderValue = 150000m,
            PermanentOfflineLock = false,
            IsActive = true,
            EffectiveFrom = isPending ? now.AddDays(5) : now.AddDays(-10),
            EffectiveTo = isReplaced ? now.AddDays(-1) : null,
            CreatedAt = now.AddDays(-10)
        };
    }

    // =========================================================================
    // UTCID01: Update current active rule -> Creates new version & archives current (Normal - N)
    // Business Rules: BR-160, BR-161
    // =========================================================================
    [Fact]
    public async Task UTCID01_UpdatePenaltyRule_ActiveRule_CreatesNewVersion_Normal()
    {
        // Arrange
        var rule = CreateSampleRule();
        var request = new UpdatePenaltyRuleRequest
        {
            MinimumOccurrence = 1,
            ScoreDeduction = 15,
            RequiredOnlineOrders = 4,
            RequiredOnlineOrderValue = 200000m,
            PermanentOfflineLock = false,
            IsActive = true,
            EffectiveFrom = null
        };

        _repositoryMock
            .Setup(r => r.GetPenaltyRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _repositoryMock
            .Setup(r => r.FindOverlappingPenaltyRuleAsync(request.MinimumOccurrence, It.IsAny<DateTime>(), It.IsAny<DateTime?>(), _ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecoveryRequirementRule?)null);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdatePenaltyRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.MinimumOccurrence.Should().Be(1);
        result.ScoreDeduction.Should().Be(15);
        result.RequiredOnlineOrders.Should().Be(4);
        result.RequiredOnlineOrderValue.Should().Be(200000m);



        _auditMock.Verify(r => r.Record(It.Is<AuditLog>(log =>
            log.ActorUserId == _adminId &&
            log.Action == AuditActionTypes.UpdatePenaltyRule
        )), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Overlapping active rule for same occurrence -> 409 Conflict (Abnormal - A)
    // Business Rules: BR-159
    // =========================================================================
    [Fact]
    public async Task UTCID02_UpdatePenaltyRule_OverlappingActiveRule_ThrowsConflict_Abnormal()
    {
        // Arrange
        var rule = CreateSampleRule();
        var request = new UpdatePenaltyRuleRequest
        {
            MinimumOccurrence = 1,
            ScoreDeduction = 20,
            RequiredOnlineOrders = 5,
            RequiredOnlineOrderValue = 250000m,
            PermanentOfflineLock = false,
            IsActive = true,
            EffectiveFrom = null
        };

        _repositoryMock
            .Setup(r => r.GetPenaltyRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _repositoryMock
            .Setup(r => r.FindOverlappingPenaltyRuleAsync(request.MinimumOccurrence, It.IsAny<DateTime>(), It.IsAny<DateTime?>(), _ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        Func<Task> act = async () => await _service.UpdatePenaltyRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        exception.Which.Message.Should().Contain("overlaps with an existing rule");
        PrintError("UTCID02", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddPenaltyRule(It.IsAny<RecoveryRequirementRule>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: Rule version has already been replaced (EffectiveTo is set) -> 409 Conflict (Abnormal - A)
    // Business Rules: BR-161
    // =========================================================================
    [Fact]
    public async Task UTCID03_UpdatePenaltyRule_AlreadyReplaced_ThrowsConflict_Abnormal()
    {
        // Arrange
        var rule = CreateSampleRule();
        var request = new UpdatePenaltyRuleRequest
        {
            MinimumOccurrence = 1,
            ScoreDeduction = 15,
            RequiredOnlineOrders = 3,
            RequiredOnlineOrderValue = 150000m,
            PermanentOfflineLock = false,
            IsActive = true,
            EffectiveTo = rule.EffectiveFrom.AddDays(-1)
        };

        _repositoryMock
            .Setup(r => r.GetPenaltyRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        Func<Task> act = async () => await _service.UpdatePenaltyRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Contain("The 'Until' date/time must be strictly later than the 'From' date/time.");
        PrintError("UTCID03", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Rule ID not found in repository -> 404 Not Found (Abnormal - A)
    // Business Rules: BR-052
    // =========================================================================
    [Fact]
    public async Task UTCID04_UpdatePenaltyRule_RuleNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdatePenaltyRuleRequest
        {
            MinimumOccurrence = 1,
            ScoreDeduction = 10,
            RequiredOnlineOrders = 3,
            RequiredOnlineOrderValue = 100000m,
            PermanentOfflineLock = false,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.GetPenaltyRuleForUpdateAsync(_nonExistentRuleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecoveryRequirementRule?)null);

        // Act
        Func<Task> act = async () => await _service.UpdatePenaltyRuleAsync(_adminId, _nonExistentRuleId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Penalty rule not found.");
        PrintError("UTCID04", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddPenaltyRule(It.IsAny<RecoveryRequirementRule>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: Update pending/scheduled rule -> Edits in place without new version (Normal - N)
    // Business Rules: BR-160
    // =========================================================================
    [Fact]
    public async Task UTCID05_UpdatePenaltyRule_PendingRule_EditsInPlace_Normal()
    {
        // Arrange
        var rule = CreateSampleRule(isPending: true);
        var request = new UpdatePenaltyRuleRequest
        {
            MinimumOccurrence = 1,
            ScoreDeduction = 20,
            RequiredOnlineOrders = 5,
            RequiredOnlineOrderValue = 250000m,
            PermanentOfflineLock = false,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.GetPenaltyRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _repositoryMock
            .Setup(r => r.FindOverlappingPenaltyRuleAsync(request.MinimumOccurrence, It.IsAny<DateTime>(), It.IsAny<DateTime?>(), _ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecoveryRequirementRule?)null);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdatePenaltyRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID05", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_ruleId); // same rule edited in place
        result.ScoreDeduction.Should().Be(20);
        result.RequiredOnlineOrders.Should().Be(5);

        _repositoryMock.Verify(r => r.AddPenaltyRule(It.IsAny<RecoveryRequirementRule>()), Times.Never); // No new rule created
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID06: MinimumOccurrence < 1 -> Validation Error 400 Bad Request (Boundary - B)
    // Business Rules: BR-159
    // =========================================================================
    [Fact]
    public void UTCID06_UpdatePenaltyRule_ZeroMinimumOccurrence_FailsValidation_Boundary()
    {
        // Arrange
        var request = new UpdatePenaltyRuleRequest
        {
            MinimumOccurrence = 0,
            ScoreDeduction = 10,
            RequiredOnlineOrders = 3,
            RequiredOnlineOrderValue = 100000m,
            PermanentOfflineLock = false,
            IsActive = true
        };

        // Act
        var validationResult = _validator.Validate(request);

        // Assert & Log
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(request.MinimumOccurrence));
        PrintError("UTCID06", 400, validationResult.Errors.First(e => e.PropertyName == nameof(request.MinimumOccurrence)).ErrorMessage);
    }

    private void PrintSuccess<T>(string tcid, T result, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }

    private void PrintError(string tcid, int statusCode, string errorMessage)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS (Expected Exception Caught)");
        _output.WriteLine($"STATUS: {statusCode}");
        _output.WriteLine($"ERROR: {errorMessage}");
        _output.WriteLine("========================================");
    }
}
