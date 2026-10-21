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
/// Unit Tests for Feature: View Level Requirements (UC-131)
/// Service: TrustConfigService.GetLevelsAsync
/// Function Code: UT_ViewLevelRequirements
/// Sheet Name: UT_ViewLevelRequirements
/// Business Rules: BR-051, BR-157, BR-158, BR-173
/// </summary>
public class ViewLevelRequirementsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly TrustConfigService _service;

    private readonly Guid _level1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _level2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _level3Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public ViewLevelRequirementsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustConfigService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private (TrustLevel Level1, TrustLevel Level2, TrustLevel Level3) CreateSampleLevels()
    {
        var level1 = new TrustLevel
        {
            Id = _level1Id,
            Name = "Bronze Member",
            Description = "Starting tier",
            Rank = 1,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-10),
            Requirement = new TrustLevelRequirement
            {
                Id = Guid.NewGuid(),
                TrustLevelId = _level1Id,
                MinScore = 0,
                MinCompletedOrders = 0,
                MinTotalOrderValue = 0m
            }
        };

        var level2 = new TrustLevel
        {
            Id = _level2Id,
            Name = "Silver Member",
            Description = "Loyal tier",
            Rank = 2,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            Requirement = new TrustLevelRequirement
            {
                Id = Guid.NewGuid(),
                TrustLevelId = _level2Id,
                MinScore = 80,
                MinCompletedOrders = 5,
                MinTotalOrderValue = 200000m
            }
        };

        var level3 = new TrustLevel
        {
            Id = _level3Id,
            Name = "Gold Member",
            Description = "VIP tier",
            Rank = 3,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
            Requirement = new TrustLevelRequirement
            {
                Id = Guid.NewGuid(),
                TrustLevelId = _level3Id,
                MinScore = 90,
                MinCompletedOrders = 15,
                MinTotalOrderValue = 500000m
            }
        };

        return (level1, level2, level3);
    }

    // =========================================================================
    // UTCID01: Retrieve trust levels with progressive requirement thresholds (Normal - N)
    // Business Rules: BR-051, BR-157, BR-158, BR-173
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetLevels_WithRequirements_ReturnsAllLevelsAndThresholds_Normal()
    {
        // Arrange
        var (level1, level2, level3) = CreateSampleLevels();
        var levels = new List<TrustLevel> { level1, level2, level3 };
        var userCounts = new Dictionary<Guid, int>
        {
            { _level1Id, 50 },
            { _level2Id, 20 },
            { _level3Id, 5 }
        };

        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(levels);

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCounts);

        // Act
        var result = await _service.GetLevelsAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var r1 = result[0];
        r1.Id.Should().Be(_level1Id);
        r1.Name.Should().Be("Bronze Member");
        r1.Rank.Should().Be(1);
        r1.Requirement.Should().NotBeNull();
        r1.Requirement!.MinScore.Should().Be(0);
        r1.Requirement.MinCompletedOrders.Should().Be(0);
        r1.Requirement.MinTotalOrderValue.Should().Be(0m);
        r1.UserCount.Should().Be(50);

        var r2 = result[1];
        r2.Id.Should().Be(_level2Id);
        r2.Requirement!.MinScore.Should().Be(80);
        r2.Requirement.MinCompletedOrders.Should().Be(5);
        r2.Requirement.MinTotalOrderValue.Should().Be(200000m);
        r2.UserCount.Should().Be(20);

        var r3 = result[2];
        r3.Id.Should().Be(_level3Id);
        r3.Requirement!.MinScore.Should().Be(90);
        r3.Requirement.MinCompletedOrders.Should().Be(15);
        r3.Requirement.MinTotalOrderValue.Should().Be(500000m);
        r3.UserCount.Should().Be(5);
    }

    // =========================================================================
    // UTCID02: Level with null requirement -> Handled gracefully with null requirement (Normal - N)
    // Business Rules: BR-051
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetLevels_LevelWithNullRequirement_ReturnsNullRequirementResponse_Normal()
    {
        // Arrange
        var level = new TrustLevel
        {
            Id = _level1Id,
            Name = "Unrestricted Tier",
            Rank = 1,
            IsActive = true,
            Requirement = null
        };

        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level });

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        // Act
        var result = await _service.GetLevelsAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Requirement.Should().BeNull();
    }

    // =========================================================================
    // UTCID03: No levels configured in system -> Returns empty list (Normal - N)
    // Business Rules: BR-158
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetLevels_EmptyList_ReturnsEmptyCollection_Normal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel>());

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        // Act
        var result = await _service.GetLevelsAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // =========================================================================
    // UTCID04: Level without user count entry in dictionary -> Defaults UserCount to 0 (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetLevels_NoUserCountEntry_DefaultsToZero_Normal()
    {
        // Arrange
        var (level1, _, _) = CreateSampleLevels();
        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1 });

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>()); // empty counts

        // Act
        var result = await _service.GetLevelsAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID04", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].UserCount.Should().Be(0);
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
