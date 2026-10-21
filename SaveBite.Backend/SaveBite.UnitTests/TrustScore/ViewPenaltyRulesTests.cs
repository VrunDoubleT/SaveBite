using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.TrustScore;

/// <summary>
/// Unit Tests for Feature: View Penalty Rules (UC-133)
/// Service: TrustConfigService.GetPenaltyRulesAsync
/// Function Code: UT_ViewPenaltyRules
/// Sheet Name: UT_ViewPenaltyRules
/// Business Rules: BR-052, BR-159, BR-160, BR-161
/// </summary>
public class ViewPenaltyRulesTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly TrustConfigService _service;

    private readonly Guid _rule1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _rule2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public ViewPenaltyRulesTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustConfigService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private (RecoveryRequirementRule Rule1, RecoveryRequirementRule Rule2) CreateSampleRules()
    {
        var now = DateTime.UtcNow;

        var rule1 = new RecoveryRequirementRule
        {
            Id = _rule1Id,
            MinimumOccurrence = 1,
            ScoreDeduction = 10,
            RequiredOnlineOrders = 3,
            RequiredOnlineOrderValue = 150000m,
            PermanentOfflineLock = false,
            IsActive = true,
            EffectiveFrom = now.AddDays(-10),
            EffectiveTo = null,
            CreatedAt = now.AddDays(-10),
            CreatedByUser = new User { FullName = "Admin John" }
        };

        var rule2 = new RecoveryRequirementRule
        {
            Id = _rule2Id,
            MinimumOccurrence = 2,
            ScoreDeduction = 25,
            RequiredOnlineOrders = 5,
            RequiredOnlineOrderValue = 300000m,
            PermanentOfflineLock = true,
            IsActive = true,
            EffectiveFrom = now.AddDays(-5),
            EffectiveTo = null,
            CreatedAt = now.AddDays(-5),
            CreatedByUser = new User { FullName = "Admin Alice" }
        };

        return (rule1, rule2);
    }

    // =========================================================================
    // UTCID01: Retrieve configured penalty rules -> Returns mapped rule details (Normal - N)
    // Business Rules: BR-052, BR-159, BR-160
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetPenaltyRules_HasRules_ReturnsConfiguredRules_Normal()
    {
        // Arrange
        var (rule1, rule2) = CreateSampleRules();
        _repositoryMock
            .Setup(r => r.GetPenaltyRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecoveryRequirementRule> { rule1, rule2 });

        // Act
        var result = await _service.GetPenaltyRulesAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        result[0].Id.Should().Be(_rule1Id);
        result[0].MinimumOccurrence.Should().Be(1);
        result[0].ScoreDeduction.Should().Be(10);
        result[0].RequiredOnlineOrders.Should().Be(3);
        result[0].RequiredOnlineOrderValue.Should().Be(150000m);
        result[0].PermanentOfflineLock.Should().BeFalse();
        result[0].IsActive.Should().BeTrue();
        result[0].IsCurrent.Should().BeTrue();
        result[0].CreatedByName.Should().Be("Admin John");

        result[1].Id.Should().Be(_rule2Id);
        result[1].MinimumOccurrence.Should().Be(2);
        result[1].ScoreDeduction.Should().Be(25);
        result[1].PermanentOfflineLock.Should().BeTrue();
        result[1].CreatedByName.Should().Be("Admin Alice");
    }

    // =========================================================================
    // UTCID02: No penalty rules configured in repository -> Returns empty list (Normal - N)
    // Business Rules: BR-159
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetPenaltyRules_NoRules_ReturnsEmptyList_Normal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPenaltyRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecoveryRequirementRule>());

        // Act
        var result = await _service.GetPenaltyRulesAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().BeEmpty();
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
}
