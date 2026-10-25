using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.TrustScore;

/// <summary>
/// Unit Tests for Feature: Update Trust Score Earning Rules (UC-138)
/// Service: TrustConfigService.UpdateEarningRuleAsync & UpdateTrustScoreRuleRequestValidator
/// Function Code: UT_UpdateTrustScoreEarningRules
/// Sheet Name: UT_UpdateTrustScoreEarningRules
/// Business Rules: BR-049, BR-160, BR-161, BR-162
/// </summary>
public class UpdateTrustScoreEarningRulesTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly TrustConfigService _service;
    private readonly UpdateTrustScoreRuleRequestValidator _validator = new();

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _ruleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _nonExistentRuleId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public UpdateTrustScoreEarningRulesTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustConfigService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private TrustScoreRule CreateSampleEarningRule(int scoreDelta = 5, bool isPending = false, bool isReplaced = false)
    {
        var now = DateTime.UtcNow;
        return new TrustScoreRule
        {
            Id = _ruleId,
            RuleType = TrustScoreRuleType.OnlineOrderCompleted,
            ScoreDelta = scoreDelta,
            EffectiveFrom = isPending ? now.AddDays(5) : now.AddDays(-10),
            EffectiveTo = isReplaced ? now.AddDays(-1) : null,
            CreatedAt = now.AddDays(-10)
        };
    }

    // =========================================================================
    // UTCID01: Update active earning rule -> Creates new version & archives current (Normal - N)
    // Business Rules: BR-160, BR-161
    // =========================================================================
    [Fact]
    public async Task UTCID01_UpdateEarningRule_ActiveRule_CreatesNewVersion_Normal()
    {
        // Arrange
        var rule = CreateSampleEarningRule(scoreDelta: 5);
        var request = new UpdateTrustScoreRuleRequest
        {
            ScoreDelta = 10,
            EffectiveFrom = null
        };

        _repositoryMock
            .Setup(r => r.GetScoreRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _repositoryMock
            .Setup(r => r.HasOverlappingScoreRuleAsync(It.IsAny<TrustScoreRuleType>(), It.IsAny<DateTime>(), _ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateEarningRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.ScoreDelta.Should().Be(10);
        result.Kind.Should().Be("Earning");

        rule.EffectiveTo.Should().NotBeNull();

        _repositoryMock.Verify(r => r.AddScoreRule(It.Is<TrustScoreRule>(r =>
            r.ScoreDelta == 10 &&
            r.CreatedBy == _adminId
        )), Times.Once);

        _repositoryMock.Verify(r => r.AddAuditLog(It.Is<AuditLog>(log =>
            log.ActorUserId == _adminId &&
            log.Action == "UpdateTrustEarningRule"
        )), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Targeted rule has negative delta (Penalty rule) -> 404 Not Found (Abnormal - A)
    // Business Rules: BR-162
    // =========================================================================
    [Fact]
    public async Task UTCID02_UpdateEarningRule_PenaltyRuleTargeted_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var penaltyRule = CreateSampleEarningRule(scoreDelta: -10);
        var request = new UpdateTrustScoreRuleRequest { ScoreDelta = 5 };

        _repositoryMock
            .Setup(r => r.GetScoreRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(penaltyRule);

        // Act
        Func<Task> act = async () => await _service.UpdateEarningRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Earning rule not found.");
        PrintError("UTCID02", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddScoreRule(It.IsAny<TrustScoreRule>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: Sign mismatch (submitting negative delta to earning rule) -> 400 Bad Request (Abnormal - A)
    // Business Rules: BR-162
    // =========================================================================
    [Fact]
    public async Task UTCID03_UpdateEarningRule_NegativeDelta_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var rule = CreateSampleEarningRule(scoreDelta: 5);
        var request = new UpdateTrustScoreRuleRequest { ScoreDelta = -5 };

        _repositoryMock
            .Setup(r => r.GetScoreRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        Func<Task> act = async () => await _service.UpdateEarningRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Contain("An earning rule must stay positive and a penalty rule must stay negative.");
        PrintError("UTCID03", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddScoreRule(It.IsAny<TrustScoreRule>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Overlapping active earning rule in timeframe -> 409 Conflict (Abnormal - A)
    // Business Rules: BR-161
    // =========================================================================
    [Fact]
    public async Task UTCID04_UpdateEarningRule_OverlappingRule_ThrowsConflict_Abnormal()
    {
        // Arrange
        var rule = CreateSampleEarningRule(scoreDelta: 5);
        var request = new UpdateTrustScoreRuleRequest { ScoreDelta = 15 };

        _repositoryMock
            .Setup(r => r.GetScoreRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _repositoryMock
            .Setup(r => r.HasOverlappingScoreRuleAsync(It.IsAny<TrustScoreRuleType>(), It.IsAny<DateTime>(), _ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _service.UpdateEarningRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        exception.Which.Message.Should().Contain("Another Trust Score rule of the same type is already in effect");
        PrintError("UTCID04", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddScoreRule(It.IsAny<TrustScoreRule>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: Rule version has already been replaced -> 409 Conflict (Abnormal - A)
    // Business Rules: BR-161
    // =========================================================================
    [Fact]
    public async Task UTCID05_UpdateEarningRule_AlreadyReplaced_ThrowsConflict_Abnormal()
    {
        // Arrange
        var rule = CreateSampleEarningRule(scoreDelta: 5, isReplaced: true);
        var request = new UpdateTrustScoreRuleRequest { ScoreDelta = 15 };

        _repositoryMock
            .Setup(r => r.GetScoreRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        Func<Task> act = async () => await _service.UpdateEarningRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
        exception.Which.Message.Should().Contain("This rule version has been replaced");
        PrintError("UTCID05", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddScoreRule(It.IsAny<TrustScoreRule>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID06: Score delta identical to current rule -> 400 Bad Request (Abnormal - A)
    // Business Rules: BR-049
    // =========================================================================
    [Fact]
    public async Task UTCID06_UpdateEarningRule_IdenticalScoreDelta_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var rule = CreateSampleEarningRule(scoreDelta: 5);
        var request = new UpdateTrustScoreRuleRequest { ScoreDelta = 5 };

        _repositoryMock
            .Setup(r => r.GetScoreRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        Func<Task> act = async () => await _service.UpdateEarningRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Contain("The score change is the same as the current rule.");
        PrintError("UTCID06", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddScoreRule(It.IsAny<TrustScoreRule>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID07: Rule ID not found in database -> 404 Not Found (Abnormal - A)
    // Business Rules: BR-049
    // =========================================================================
    [Fact]
    public async Task UTCID07_UpdateEarningRule_RuleNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateTrustScoreRuleRequest { ScoreDelta = 10 };

        _repositoryMock
            .Setup(r => r.GetScoreRuleForUpdateAsync(_nonExistentRuleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrustScoreRule?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateEarningRuleAsync(_adminId, _nonExistentRuleId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Earning rule not found.");
        PrintError("UTCID07", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddScoreRule(It.IsAny<TrustScoreRule>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID08: Update pending earning rule -> Edits in place without new version (Normal - N)
    // Business Rules: BR-160
    // =========================================================================
    [Fact]
    public async Task UTCID08_UpdateEarningRule_PendingRule_EditsInPlace_Normal()
    {
        // Arrange
        var rule = CreateSampleEarningRule(scoreDelta: 5, isPending: true);
        var request = new UpdateTrustScoreRuleRequest { ScoreDelta = 8 };

        _repositoryMock
            .Setup(r => r.GetScoreRuleForUpdateAsync(_ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        _repositoryMock
            .Setup(r => r.HasOverlappingScoreRuleAsync(It.IsAny<TrustScoreRuleType>(), It.IsAny<DateTime>(), _ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateEarningRuleAsync(_adminId, _ruleId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID08", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_ruleId);
        result.ScoreDelta.Should().Be(8);

        _repositoryMock.Verify(r => r.AddScoreRule(It.IsAny<TrustScoreRule>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID09: ScoreDelta = 0 -> Validation Error 400 Bad Request (Boundary - B)
    // Business Rules: BR-162
    // =========================================================================
    [Fact]
    public void UTCID09_UpdateEarningRule_ZeroScoreDelta_FailsValidation_Boundary()
    {
        // Arrange
        var request = new UpdateTrustScoreRuleRequest { ScoreDelta = 0 };

        // Act
        var validationResult = _validator.Validate(request);

        // Assert & Log
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(request.ScoreDelta));
        PrintError("UTCID09", 400, validationResult.Errors.First(e => e.PropertyName == nameof(request.ScoreDelta)).ErrorMessage);
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
