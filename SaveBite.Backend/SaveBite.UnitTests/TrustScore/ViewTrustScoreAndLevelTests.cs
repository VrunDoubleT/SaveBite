using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.TrustScore;

/// <summary>
/// Unit Tests for Feature: View Trust Score and Level (UC-44)
/// Service: TrustScoreService.GetOverviewAsync
/// Function Code: UT_ViewTrustScoreAndLevel
/// Sheet Name: UT_ViewTrustScoreAndLevel
/// Business Rules: BR-021, BR-050, BR-051, BR-054, BR-158, BR-173
/// </summary>
public class ViewTrustScoreAndLevelTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustScoreRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustScoreService>> _loggerMock = new();
    private readonly TrustScoreService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _level1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _level2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public ViewTrustScoreAndLevelTests(ITestOutputHelper output)
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
            Name = "Thanh vien Dong",
            Description = "Cap do khoi dau",
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
            Name = "Thanh vien Bac",
            Description = "Cap do than thiet",
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
    // TCID01: Người dùng đã có điểm uy tín, không có phạt, không khoá offline -> Trả về tổng quan với trạng thái GoodStanding (Normal - N)
    // Business Rules: BR-050, BR-051, BR-173
    // =========================================================================
    [Fact]
    public async Task TCID01_GetOverview_UserExists_GoodStanding_ReturnsOverview_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level1.Id,
            TrustLevel = level1,
            CurrentScore = 100,
            IsOfflineLockedPermanently = false,
            UpdatedAt = DateTime.UtcNow
        };
        var progress = new UserLevelProgress
        {
            UserId = _userId,
            CompletedOrdersCount = 2,
            AccumulatedOrderValue = 50000m
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
            .Setup(r => r.CountRecoveringPenaltiesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetOverviewAsync(_userId);

        // Assert & Log
        PrintSuccess("TCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.CurrentScore.Should().Be(100);
        result.Level.Name.Should().Be("Thanh vien Dong");
        result.Level.Rank.Should().Be(1);
        result.IsHighestLevel.Should().BeFalse();
        result.IsOfflineLockedPermanently.Should().BeFalse();
        result.CompletedOrdersCount.Should().Be(2);
        result.AccumulatedOrderValue.Should().Be(50000m);
        result.ActivePenaltyCount.Should().Be(0);
        result.Standing.Should().Be(TrustScoreService.StandingGood);
    }

    // =========================================================================
    // TCID02: Người dùng đạt cấp độ cao nhất -> IsHighestLevel = true (Normal - N)
    // Business Rules: BR-050, BR-051
    // =========================================================================
    [Fact]
    public async Task TCID02_GetOverview_UserAtHighestLevel_ReturnsIsHighestLevelTrue_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level2.Id,
            TrustLevel = level2,
            CurrentScore = 95,
            IsOfflineLockedPermanently = false,
            UpdatedAt = DateTime.UtcNow
        };
        var progress = new UserLevelProgress
        {
            UserId = _userId,
            CompletedOrdersCount = 10,
            AccumulatedOrderValue = 500000m
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
            .Setup(r => r.CountRecoveringPenaltiesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetOverviewAsync(_userId);

        // Assert & Log
        PrintSuccess("TCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.Level.Rank.Should().Be(2);
        result.IsHighestLevel.Should().BeTrue();
        result.Standing.Should().Be(TrustScoreService.StandingGood);
    }

    // =========================================================================
    // TCID03: Người dùng đang có vi phạm cần khắc phục -> Standing = Restricted (Normal - N)
    // Business Rules: BR-050, BR-054
    // =========================================================================
    [Fact]
    public async Task TCID03_GetOverview_HasActivePenalties_ReturnsRestrictedStanding_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level1.Id,
            TrustLevel = level1,
            CurrentScore = 75,
            IsOfflineLockedPermanently = false,
            UpdatedAt = DateTime.UtcNow
        };
        var progress = new UserLevelProgress { UserId = _userId };

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
            .Setup(r => r.CountRecoveringPenaltiesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _service.GetOverviewAsync(_userId);

        // Assert & Log
        PrintSuccess("TCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.ActivePenaltyCount.Should().Be(2);
        result.Standing.Should().Be(TrustScoreService.StandingRestricted);
    }

    // =========================================================================
    // TCID04: Người dùng bị khoá thanh toán tại quán vĩnh viễn -> Standing = OfflineLocked (Normal - N)
    // Business Rules: BR-021, BR-050
    // =========================================================================
    [Fact]
    public async Task TCID04_GetOverview_OfflineLockedPermanently_ReturnsOfflineLockedStanding_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level1.Id,
            TrustLevel = level1,
            CurrentScore = 30,
            IsOfflineLockedPermanently = true,
            UpdatedAt = DateTime.UtcNow
        };
        var progress = new UserLevelProgress { UserId = _userId };

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
            .Setup(r => r.CountRecoveringPenaltiesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.GetOverviewAsync(_userId);

        // Assert & Log
        PrintSuccess("TCID04", result, "200 OK");
        result.Should().NotBeNull();
        result.IsOfflineLockedPermanently.Should().BeTrue();
        result.Standing.Should().Be(TrustScoreService.StandingOfflineLocked);
    }

    // =========================================================================
    // TCID05: Người dùng lần đầu truy cập chưa có điểm uy tín -> Tự động khởi tạo cấp độ thấp nhất với 100 điểm (Normal - N)
    // Business Rules: BR-050, BR-158
    // =========================================================================
    [Fact]
    public async Task TCID05_GetOverview_UserNotProvisioned_AutoProvisionsToLowestLevel_Normal()
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
            IsOfflineLockedPermanently = false,
            UpdatedAt = DateTime.UtcNow
        };

        // First call returns null, second call returns provisioned score
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
            .Setup(r => r.CountRecoveringPenaltiesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _service.InitializeAsync(_userId);
        var result = await _service.GetOverviewAsync(_userId);

        // Assert & Log
        PrintSuccess("TCID05", result, "200 OK");
        result.Should().NotBeNull();
        result.CurrentScore.Should().Be(100);
        result.Level.Name.Should().Be("Thanh vien Dong");
        result.Standing.Should().Be(TrustScoreService.StandingGood);

        _repositoryMock.Verify(
            r => r.TryProvisionAsync(_userId, level1.Id, 0, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // TCID06: Người dùng chưa khởi tạo và hệ thống chưa cấu hình cấp độ nào -> Ném ServiceUnavailableException (Abnormal - A)
    // Business Rules: BR-158
    // =========================================================================
    [Fact]
    public async Task TCID06_GetOverview_UserNotProvisioned_NoLevelsConfigured_ThrowsServiceUnavailable_Abnormal()
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

        _output.WriteLine("================ TCID06 ================");
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
