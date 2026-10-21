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
/// Unit Tests for Feature: View Level Info & Benefits (UC-47)
/// Service: TrustScoreService.GetLevelsAsync
/// Function Code: UT_ViewLevelInfoAndBenefits
/// Sheet Name: UT_ViewLevelInfoAndBenefits
/// Business Rules: BR-050, BR-051, BR-158
/// </summary>
public class ViewLevelInfoAndBenefitsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustScoreRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustScoreService>> _loggerMock = new();
    private readonly TrustScoreService _service;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _level1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _level2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _level3Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public ViewLevelInfoAndBenefitsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustScoreService(
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

        var level3 = new TrustLevel
        {
            Id = _level3Id,
            Name = "Thành viên Vàng",
            Description = "Cấp độ VIP",
            Rank = 3,
            IsActive = true,
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
    // UTCID01: Người dùng ở cấp độ thấp nhất (Rank 1) -> Chỉ cấp độ 1 được unlock và là hiện tại (Normal - N)
    // Business Rules: BR-050, BR-051
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetLevels_UserAtLowestLevel_ReturnsCorrectUnlockedStatus_Normal()
    {
        // Arrange
        var (level1, level2, level3) = CreateSampleLevels();
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

        _repositoryMock
            .Setup(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);
        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2, level3 });

        // Act
        var result = await _service.GetLevelsAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var rLevel1 = result.First(l => l.Id == level1.Id);
        rLevel1.IsCurrent.Should().BeTrue();
        rLevel1.IsUnlocked.Should().BeTrue();

        var rLevel2 = result.First(l => l.Id == level2.Id);
        rLevel2.IsCurrent.Should().BeFalse();
        rLevel2.IsUnlocked.Should().BeFalse();

        var rLevel3 = result.First(l => l.Id == level3.Id);
        rLevel3.IsCurrent.Should().BeFalse();
        rLevel3.IsUnlocked.Should().BeFalse();
    }

    // =========================================================================
    // UTCID02: Người dùng ở cấp độ trung gian (Rank 2) -> Cấp 1 và 2 unlocked, cấp 2 là hiện tại (Normal - N)
    // Business Rules: BR-050, BR-051
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetLevels_UserAtMiddleLevel_ReturnsUnlockedUpToCurrentRank_Normal()
    {
        // Arrange
        var (level1, level2, level3) = CreateSampleLevels();
        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level2.Id,
            TrustLevel = level2,
            CurrentScore = 85,
            IsOfflineLockedPermanently = false,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);
        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2, level3 });

        // Act
        var result = await _service.GetLevelsAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var rLevel1 = result.First(l => l.Id == level1.Id);
        rLevel1.IsCurrent.Should().BeFalse();
        rLevel1.IsUnlocked.Should().BeTrue();

        var rLevel2 = result.First(l => l.Id == level2.Id);
        rLevel2.IsCurrent.Should().BeTrue();
        rLevel2.IsUnlocked.Should().BeTrue();

        var rLevel3 = result.First(l => l.Id == level3.Id);
        rLevel3.IsCurrent.Should().BeFalse();
        rLevel3.IsUnlocked.Should().BeFalse();
    }

    // =========================================================================
    // UTCID03: Người dùng ở cấp độ cao nhất (Rank 3) -> Tất cả cấp độ đều unlocked, cấp 3 là hiện tại (Normal - N)
    // Business Rules: BR-050, BR-051
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetLevels_UserAtHighestLevel_AllLevelsUnlocked_Normal()
    {
        // Arrange
        var (level1, level2, level3) = CreateSampleLevels();
        var score = new UserTrustScore
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            TrustLevelId = level3.Id,
            TrustLevel = level3,
            CurrentScore = 95,
            IsOfflineLockedPermanently = false,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);
        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2, level3 });

        // Act
        var result = await _service.GetLevelsAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        result.All(l => l.IsUnlocked).Should().BeTrue();
        result.Single(l => l.IsCurrent).Id.Should().Be(level3.Id);
    }

    // =========================================================================
    // UTCID04: Người dùng lần đầu truy cập chưa có điểm uy tín -> Tự động khởi tạo cấp độ thấp nhất và trả về danh sách (Normal - N)
    // Business Rules: BR-050, BR-158
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetLevels_UserNotProvisioned_AutoProvisionsAndReturnsLevels_Normal()
    {
        // Arrange
        var (level1, level2, level3) = CreateSampleLevels();
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

        _repositoryMock
            .SetupSequence(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserTrustScore?)null)
            .ReturnsAsync(provisionedScore);

        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2, level3 });

        _repositoryMock
            .Setup(r => r.TryProvisionAsync(_userId, level1.Id, 100, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.GetLevelsAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID04", result, "200 OK");
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var rLevel1 = result.First(l => l.Id == level1.Id);
        rLevel1.IsCurrent.Should().BeTrue();
        rLevel1.IsUnlocked.Should().BeTrue();

        _repositoryMock.Verify(
            r => r.TryProvisionAsync(_userId, level1.Id, 100, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // UTCID05: Người dùng chưa khởi tạo và hệ thống chưa cấu hình cấp độ nào -> Ném ServiceUnavailableException (Abnormal - A)
    // Business Rules: BR-158
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetLevels_UserNotProvisioned_NoLevelsConfigured_ThrowsServiceUnavailable_Abnormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetUserTrustScoreAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserTrustScore?)null);
        _repositoryMock
            .Setup(r => r.GetActiveLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel>());

        // Act
        Func<Task> act = async () => await _service.GetLevelsAsync(_userId);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Trust levels are not configured yet.");

        _output.WriteLine("================ UTCID05 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 503 Service Unavailable");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
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
