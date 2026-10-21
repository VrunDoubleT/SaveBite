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
/// Unit Tests for Feature: View Trust Score Earning Rules (UC-137)
/// Service: TrustConfigService.GetEarningRulesAsync
/// Function Code: UT_ViewTrustScoreEarningRules
/// Sheet Name: UT_ViewTrustScoreEarningRules
/// Business Rules: BR-049, BR-162, BR-163
/// </summary>
public class ViewTrustScoreEarningRulesTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly TrustConfigService _service;

    private readonly Guid _rule1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _rule2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public ViewTrustScoreEarningRulesTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustConfigService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private (TrustScoreRule Rule1, TrustScoreRule Rule2) CreateSampleEarningRules()
    {
        var now = DateTime.UtcNow;

        var rule1 = new TrustScoreRule
        {
            Id = _rule1Id,
            ScoreDelta = 5,
            EffectiveFrom = now.AddDays(-10),
            EffectiveTo = null,
            CreatedAt = now.AddDays(-10),
            CreatedByUser = new User { FullName = "Admin Bob" }
        };

        var rule2 = new TrustScoreRule
        {
            Id = _rule2Id,
            ScoreDelta = 10,
            EffectiveFrom = now.AddDays(-5),
            EffectiveTo = null,
            CreatedAt = now.AddDays(-5),
            CreatedByUser = new User { FullName = "Admin Alice" }
        };

        return (rule1, rule2);
    }

    // =========================================================================
    // UTCID01: Retrieve configured earning rules -> Returns mapped earning rules (Normal - N)
    // Business Rules: BR-049, BR-162
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetEarningRules_HasRules_ReturnsConfiguredEarningRules_Normal()
    {
        // Arrange
        var (rule1, rule2) = CreateSampleEarningRules();
        _repositoryMock
            .Setup(r => r.GetScoreRulesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustScoreRule> { rule1, rule2 });

        // Act
        var result = await _service.GetEarningRulesAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        result[0].Id.Should().Be(_rule1Id);
        result[0].ScoreDelta.Should().Be(5);
        result[0].Kind.Should().Be("Earning");
        result[0].IsCurrent.Should().BeTrue();
        result[0].CreatedByName.Should().Be("Admin Bob");

        result[1].Id.Should().Be(_rule2Id);
        result[1].ScoreDelta.Should().Be(10);
        result[1].Kind.Should().Be("Earning");
        result[1].IsCurrent.Should().BeTrue();
        result[1].CreatedByName.Should().Be("Admin Alice");
    }

    // =========================================================================
    // UTCID02: No earning rules configured in repository -> Returns empty list (Normal - N)
    // Business Rules: BR-162
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetEarningRules_NoRules_ReturnsEmptyList_Normal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetScoreRulesAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustScoreRule>());

        // Act
        var result = await _service.GetEarningRulesAsync(CancellationToken.None);

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
