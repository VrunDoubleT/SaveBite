using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.TrustScore;

/// <summary>
/// Unit Tests for Feature: Track Level Progress (UC-45)
/// Service: TrustScoreService.GetLevelProgressAsync
/// Function Code: UT-61
/// Sheet Name: UT_TrackLevelProgress
/// Business Rules: BR-051, BR-052, BR-053, BR-158
/// </summary>
public class TrackLevelProgressTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustScoreRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustScoreService>> _loggerMock = new();
    private readonly TrustScoreService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _level1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _level2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public TrackLevelProgressTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustScoreService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private (TrustLevel Level1, TrustLevel Level2) CreateSampleLevels()
    {
        var level1 = new TrustLevel
        {
            Id = _level1Id,
            Name = "Thành viên Đồng",
            Description = "Cấp độ khởi đầu",
            Rank = 1,
            IsActive = true,
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
            Name = "Thành viên Bạc",
            Description = "Cấp độ thân thiết",
            Rank = 2,
            IsActive = true,
            Requirement = new TrustLevelRequirement
            {
                Id = Guid.NewGuid(),
                TrustLevelId = _level2Id,
                MinScore = 80,
                MinCompletedOrders = 5,
                MinTotalOrderValue = 200000m
            }
        };

        return (level1, level2);
    }

    // =========================================================================
    // TCID01: Người dùng đang ở cấp thấp, tiến độ một phần -> Trả về NextLevel, Metrics, RequirementsMet = false (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetLevelProgress_PartialProgress_ReturnsNextLevelAndMetrics_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level1.Id,
            TrustLevel = level1,
            CurrentScore = 60,
            UpdatedAt = DateTime.UtcNow
        };
        var progress = new UserLevelProgress
        {
            UserId = _userId,
            CompletedOrdersCount = 2,
            AccumulatedOrderValue = 100000m
        };
        var earningRule = new TrustScoreRule
        {
            Id = Guid.NewGuid(),
            ScoreDelta = 2,
            EffectiveFrom = DateTime.UtcNow.AddDays(-7),
            EffectiveTo = null
        };

        _repositoryMock
            .Setup(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);
        _repositoryMock
            .Setup(r => r.GetLevelProgressAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);
        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2 });
        _repositoryMock
            .Setup(r => r.GetCurrentEarningRuleAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>(), It.IsAny<TrustScoreRuleType>()))
            .ReturnsAsync(new TrustScoreRule { ScoreDelta = 2, RuleType = TrustScoreRuleType.OnlineOrderCompleted });
        // Act
        var result = await _service.GetLevelProgressAsync(_userId);

        // Assert & Log
        PrintSuccess("TCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.CurrentLevel.Rank.Should().Be(1);
        result.NextLevel.Should().NotBeNull();
        result.NextLevel!.Rank.Should().Be(2);
        result.IsHighestLevel.Should().BeFalse();
        result.RequirementsMet.Should().BeFalse();
        result.OverallPercent.Should().Be(55.0m);
        result.Metrics.Should().HaveCount(3);
        result.OnlineOrderCompletedPoints.Should().Be(2);
    }

    // =========================================================================
    // TCID02: Người dùng đạt đủ tất cả điều kiện lên cấp -> RequirementsMet = true (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetLevelProgress_AllRequirementsMet_ReturnsRequirementsMetTrue_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level1.Id,
            TrustLevel = level1,
            CurrentScore = 90,
            UpdatedAt = DateTime.UtcNow
        };
        var progress = new UserLevelProgress
        {
            UserId = _userId,
            CompletedOrdersCount = 6,
            AccumulatedOrderValue = 250000m
        };

        _repositoryMock
            .Setup(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);
        _repositoryMock
            .Setup(r => r.GetLevelProgressAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);
        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2 });
        _repositoryMock
            .Setup(r => r.GetCurrentEarningRuleAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>(), It.IsAny<TrustScoreRuleType>()))
            .ReturnsAsync((TrustScoreRule?)null);
        // Act
        var result = await _service.GetLevelProgressAsync(_userId);

        // Assert & Log
        PrintSuccess("TCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.IsHighestLevel.Should().BeFalse();
        result.RequirementsMet.Should().BeTrue();
        result.OverallPercent.Should().Be(100m);
        result.Metrics.All(m => m.Remaining == 0).Should().BeTrue();
    }

    // =========================================================================
    // TCID03: Người dùng đã ở cấp độ cao nhất -> NextLevel = null, IsHighestLevel = true (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetLevelProgress_UserAtHighestLevel_ReturnsHighestLevel_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level2.Id,
            TrustLevel = level2,
            CurrentScore = 100,
            UpdatedAt = DateTime.UtcNow
        };
        var progress = new UserLevelProgress
        {
            UserId = _userId,
            CompletedOrdersCount = 20,
            AccumulatedOrderValue = 1000000m
        };

        _repositoryMock
            .Setup(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);
        _repositoryMock
            .Setup(r => r.GetLevelProgressAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);
        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2 });
        _repositoryMock
            .Setup(r => r.GetCurrentEarningRuleAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>(), It.IsAny<TrustScoreRuleType>()))
            .ReturnsAsync((TrustScoreRule?)null);
        // Act
        var result = await _service.GetLevelProgressAsync(_userId);

        // Assert & Log
        PrintSuccess("TCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.NextLevel.Should().BeNull();
        result.IsHighestLevel.Should().BeTrue();
        result.RequirementsMet.Should().BeFalse();
        result.OverallPercent.Should().Be(100m);
        result.Metrics.Should().BeEmpty();
    }

    // =========================================================================
    // TCID04: Người dùng chưa khởi tạo -> Tự động khởi tạo và tính tiến độ lên cấp kế tiếp (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID04_GetLevelProgress_UserNotProvisioned_AutoProvisionsAndReturnsProgress_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var provisionedScore = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level1.Id,
            TrustLevel = level1,
            CurrentScore = 100,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .SetupSequence(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserTrustScore?)null)
            .ReturnsAsync(provisionedScore);

        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2 });
        _repositoryMock
            .Setup(r => r.TryProvisionAsync(_userId, level1.Id, 0, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(r => r.GetLevelProgressAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserLevelProgress { UserId = _userId });
        _repositoryMock
            .Setup(r => r.GetCurrentEarningRuleAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>(), It.IsAny<TrustScoreRuleType>()))
            .ReturnsAsync((TrustScoreRule?)null);
        // Act
        await _service.InitializeAsync(_userId);
        var result = await _service.GetLevelProgressAsync(_userId);

        // Assert & Log
        PrintSuccess("TCID04", result, "200 OK");
        result.Should().NotBeNull();
        result.CurrentLevel.Rank.Should().Be(1);
        result.NextLevel.Should().NotBeNull();

        _repositoryMock.Verify(
            r => r.TryProvisionAsync(_userId, level1.Id, 0, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // TCID05: Người dùng chưa khởi tạo và hệ thống chưa cấu hình cấp độ -> Ném ServiceUnavailableException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_GetLevelProgress_UserNotProvisioned_NoLevelsConfigured_ThrowsServiceUnavailable_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserTrustScore?)null);
        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel>());

        // Act
        Func<Task> act = async () => await _service.InitializeAsync(_userId);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Trust levels are not configured yet.");

        _output.WriteLine("================ TCID05 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 503 Service Unavailable");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================
    // Helper Methods for Formatting Log Output
    // =========================================================
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
