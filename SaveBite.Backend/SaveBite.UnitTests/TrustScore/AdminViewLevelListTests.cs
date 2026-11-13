using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.UnitTests.TrustScore;

public class AdminViewLevelListTests
{
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly Mock<INotificationQueue> _notificationMock = new();
    private readonly TrustConfigService _service;

    public AdminViewLevelListTests()
    {
        _service = new TrustConfigService(
            _repositoryMock.Object,
            _auditMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            _notificationMock.Object);
    }

    [Fact]
    public async Task GetLevelsAsync_WhenLevelsExist_ReturnsMappedLevelsWithUserCountsAndPolicies()
    {
        var level1Id = Guid.NewGuid();
        var level2Id = Guid.NewGuid();
        var now = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

        var levels = new List<TrustLevel>
        {
            new()
            {
                Id = level1Id,
                Name = "Newbie",
                Description = "Entry tier",
                Rank = 1,
                IsActive = true,
                UpdatedAt = now,
                Requirement = new TrustLevelRequirement
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = level1Id,
                    MinScore = 0,
                    MinCompletedOrders = 0,
                    MinTotalOrderValue = 0m
                },
                DepositPolicy = new TrustLevelDepositPolicy
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = level1Id,
                    RequireFullOnlinePayment = true,
                    UpdatedAt = now
                }
            },
            new()
            {
                Id = level2Id,
                Name = "Trusted",
                Description = "Pay at shop enabled",
                Rank = 2,
                IsActive = false,
                UpdatedAt = now,
                Requirement = new TrustLevelRequirement
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = level2Id,
                    MinScore = 80,
                    MinCompletedOrders = 5,
                    MinTotalOrderValue = 500000m
                },
                DepositPolicy = new TrustLevelDepositPolicy
                {
                    Id = Guid.NewGuid(),
                    TrustLevelId = level2Id,
                    RequireFullOnlinePayment = false,
                    DepositFreeOrderAmount = 100000m,
                    MaxDepositOrderAmount = 300000m,
                    UpdatedAt = now,
                    Ranges = new List<TrustLevelDepositRange>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            FromOrderAmount = 200000m,
                            ToOrderAmount = 300000m,
                            DepositAmount = 50000m
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            FromOrderAmount = 100000m,
                            ToOrderAmount = 200000m,
                            DepositAmount = 20000m
                        }
                    }
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(levels);
        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [level1Id] = 42 });

        var result = await _service.GetLevelsAsync();

        result.Should().HaveCount(2);

        result[0].Id.Should().Be(level1Id);
        result[0].Name.Should().Be("Newbie");
        result[0].Rank.Should().Be(1);
        result[0].IsActive.Should().BeTrue();
        result[0].UserCount.Should().Be(42);
        result[0].DepositPolicy!.RequireFullOnlinePayment.Should().BeTrue();
        result[0].Requirement!.MinScore.Should().Be(0);

        result[1].Id.Should().Be(level2Id);
        result[1].IsActive.Should().BeFalse();
        result[1].UserCount.Should().Be(0);
        result[1].DepositPolicy!.Ranges.Should().HaveCount(2);
        result[1].DepositPolicy!.Ranges.Select(r => r.FromOrderAmount)
            .Should().ContainInOrder(100000m, 200000m);
    }

    [Fact]
    public async Task GetLevelsAsync_WhenNoLevelsExist_ReturnsEmptyList()
    {
        _repositoryMock
            .Setup(r => r.GetLevelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TrustLevel>());
        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _service.GetLevelsAsync();

        result.Should().BeEmpty();
    }
}
