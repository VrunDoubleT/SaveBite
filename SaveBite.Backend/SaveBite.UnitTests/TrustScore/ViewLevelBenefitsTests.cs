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
/// Unit Tests for Feature: View Level Benefits (UC-129)
/// Service: TrustConfigService.GetLevelsAsync
/// Function Code: UT_ViewLevelBenefits
/// Sheet Name: UT_ViewLevelBenefits
/// Business Rules: BR-050, BR-051, BR-158, BR-173
/// </summary>
public class ViewLevelBenefitsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly TrustConfigService _service;

    private readonly Guid _level1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _level2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _level3Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public ViewLevelBenefitsTests(ITestOutputHelper output)
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
            Description = "Base tier: standard ordering privileges, 1 concurrent flash deal order.",
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
            Description = "Silver tier: 5% discount vouchers on rescue packs, 3 concurrent flash deal orders.",
            Rank = 2,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            Requirement = new TrustLevelRequirement
            {
                Id = Guid.NewGuid(),
                TrustLevelId = _level2Id,
                MinScore = 100,
                MinCompletedOrders = 5,
                MinTotalOrderValue = 200000m
            }
        };

        var level3 = new TrustLevel
        {
            Id = _level3Id,
            Name = "Gold Member",
            Description = "Gold tier: 10% discount vouchers, 5 concurrent flash deal orders, priority support.",
            Rank = 3,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            Requirement = new TrustLevelRequirement
            {
                Id = Guid.NewGuid(),
                TrustLevelId = _level3Id,
                MinScore = 300,
                MinCompletedOrders = 20,
                MinTotalOrderValue = 1000000m
            }
        };

        return (level1, level2, level3);
    }

    // =========================================================================
    // UTCID01: Retrieve all levels with configured benefit descriptions -> Success (Normal - N)
    // Business Rules: BR-050, BR-051, BR-158
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetLevels_ReturnsConfiguredBenefitsAndRanks_Normal()
    {
        // Arrange
        var (level1, level2, level3) = CreateSampleLevels();
        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2, level3 });

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>
            {
                { _level1Id, 150 },
                { _level2Id, 45 },
                { _level3Id, 12 }
            });

        // Act
        var result = await _service.GetLevelsAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        result[0].Id.Should().Be(_level1Id);
        result[0].Rank.Should().Be(1);
        result[0].Description.Should().Be("Base tier: standard ordering privileges, 1 concurrent flash deal order.");
        result[0].UserCount.Should().Be(150);

        result[1].Id.Should().Be(_level2Id);
        result[1].Rank.Should().Be(2);
        result[1].Description.Should().Be("Silver tier: 5% discount vouchers on rescue packs, 3 concurrent flash deal orders.");
        result[1].UserCount.Should().Be(45);

        result[2].Id.Should().Be(_level3Id);
        result[2].Rank.Should().Be(3);
        result[2].Description.Should().Be("Gold tier: 10% discount vouchers, 5 concurrent flash deal orders, priority support.");
        result[2].UserCount.Should().Be(12);
    }

    // =========================================================================
    // UTCID02: Level with unconfigured benefit description -> Returns Description as null (Normal - N)
    // Business Rules: BR-050
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetLevels_NullBenefitDescription_ReturnsNullDescription_Normal()
    {
        // Arrange
        var (level1, _, _) = CreateSampleLevels();
        level1.Description = null; // No benefit description entered yet

        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1 });

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { _level1Id, 10 } });

        // Act
        var result = await _service.GetLevelsAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Description.Should().BeNull();
        result[0].UserCount.Should().Be(10);
    }

    // =========================================================================
    // UTCID03: Levels with active and inactive statuses -> Returns all levels with status flags (Normal - N)
    // Business Rules: BR-051, BR-173
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetLevels_ActiveAndInactiveLevels_ReturnsAllWithStatusFlags_Normal()
    {
        // Arrange
        var (level1, level2, _) = CreateSampleLevels();
        level2.IsActive = false; // Inactive / Draft level tier

        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2 });

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>
            {
                { _level1Id, 50 },
                { _level2Id, 0 }
            });

        // Act
        var result = await _service.GetLevelsAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].IsActive.Should().BeTrue();
        result[1].IsActive.Should().BeFalse();
        result[1].Description.Should().NotBeNullOrWhiteSpace();
    }

    // =========================================================================
    // UTCID04: No levels configured in system -> Returns empty list (Normal - N)
    // Business Rules: BR-051
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetLevels_NoConfiguredLevels_ReturnsEmptyList_Normal()
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
        PrintSuccess("UTCID04", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // =========================================================================
    // UTCID05: Level without user count mapping -> Defaults UserCount to 0 (Normal - N)
    // Business Rules: BR-050
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetLevels_MissingUserCountMapping_DefaultsToZero_Normal()
    {
        // Arrange
        var (level1, _, _) = CreateSampleLevels();
        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1 });

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>()); // Empty dictionary (no mapped users)

        // Act
        var result = await _service.GetLevelsAsync(CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID05", result, "200 OK");
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
