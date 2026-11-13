using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Audit;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;

namespace SaveBite.UnitTests.TrustScore;

public class AddNewLevelTests
{
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly Mock<INotificationQueue> _notificationMock = new();
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero));
    private readonly TrustConfigService _service;
    private readonly CreateTrustLevelRequestValidator _validator = new();

    public AddNewLevelTests()
    {
        _repositoryMock
            .Setup(r => r.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<AdminTrustLevelResponse>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<AdminTrustLevelResponse>> action, bool _, CancellationToken ct) => action(ct));

        _service = new TrustConfigService(
            _repositoryMock.Object,
            _auditMock.Object,
            _timeProvider,
            _loggerMock.Object,
            _notificationMock.Object);
    }

    [Fact]
    public async Task CreateLevelAsync_WhenValidPayAtShopLevel_CreatesLevelWithNextRankHistoryAndAudit()
    {
        var adminId = Guid.NewGuid();
        var existingLevel = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Standard",
            Rank = 1,
            IsActive = true,
            Requirement = new TrustLevelRequirement { MinScore = 0, MinCompletedOrders = 0, MinTotalOrderValue = 0m }
        };

        _repositoryMock
            .Setup(r => r.LevelNameExistsAsync("Gold Member", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(r => r.GetAllLevelsWithRequirementsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingLevel });

        var request = new CreateTrustLevelRequest
        {
            Name = "  Gold Member  ",
            Requirement = new UpdateTrustLevelRequirementRequest
            {
                MinScore = 90,
                MinCompletedOrders = 5,
                MinTotalOrderValue = 500000m
            },
            Benefits = new UpdateTrustLevelBenefitsRequest
            {
                Description = "  VIP perks  ",
                RequireFullOnlinePayment = false,
                DepositFreeOrderAmount = 100000m,
                MaxDepositOrderAmount = 300000m,
                Ranges =
                {
                    new TrustLevelDepositRangeRequest { FromOrderAmount = 100000m, ToOrderAmount = 200000m, DepositAmount = 20000m },
                    new TrustLevelDepositRangeRequest { FromOrderAmount = 200000m, ToOrderAmount = 300000m, DepositAmount = 50000m }
                }
            }
        };

        var result = await _service.CreateLevelAsync(adminId, request);

        result.Name.Should().Be("Gold Member");
        result.Description.Should().Be("VIP perks");
        result.Rank.Should().Be(2);
        result.IsActive.Should().BeTrue();
        result.Requirement!.MinScore.Should().Be(90);
        result.DepositPolicy!.DepositFreeOrderAmount.Should().Be(100000m);
        result.DepositPolicy.Ranges.Should().HaveCount(2);

        _repositoryMock.Verify(r => r.AddLevel(It.Is<TrustLevel>(l => l.Name == "Gold Member" && l.Rank == 2 && l.IsActive)), Times.Once);
        _repositoryMock.Verify(r => r.AddRequirement(It.Is<TrustLevelRequirement>(req => req.MinScore == 90)), Times.Once);
        _repositoryMock.Verify(r => r.AddDepositPolicy(It.IsAny<TrustLevelDepositPolicy>()), Times.Once);
        _repositoryMock.Verify(r => r.AddDepositRanges(It.Is<IEnumerable<TrustLevelDepositRange>>(ranges => ranges.Count() == 2)), Times.Once);
        _repositoryMock.Verify(r => r.AddLevelHistory(It.Is<TrustLevelHistory>(h => h.VersionNo == 1 && h.ChangeType == TrustLevelChangeType.Created)), Times.Once);
        _repositoryMock.Verify(r => r.IncrementPolicyVersionAndQueueOutboxAsync("Level Gold Member created", It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.Verify(a => a.Record(It.Is<AuditLog>(log => log.Action == AuditActionTypes.CreateTrustLevel && log.ActorUserId == adminId)), Times.Once);
    }

    [Fact]
    public async Task CreateLevelAsync_WhenValidFullOnlineLevel_ClearsDepositThresholdsAndCreatesLevel()
    {
        var adminId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.LevelNameExistsAsync("Starter", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.GetAllLevelsWithRequirementsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TrustLevel>());

        var request = new CreateTrustLevelRequest
        {
            Name = "Starter",
            Requirement = new UpdateTrustLevelRequirementRequest { MinScore = 0, MinCompletedOrders = 0, MinTotalOrderValue = 0m },
            Benefits = new UpdateTrustLevelBenefitsRequest { Description = "Basic", RequireFullOnlinePayment = true }
        };

        var result = await _service.CreateLevelAsync(adminId, request);

        result.Rank.Should().Be(1);
        result.DepositPolicy!.RequireFullOnlinePayment.Should().BeTrue();
        result.DepositPolicy.DepositFreeOrderAmount.Should().BeNull();
        result.DepositPolicy.Ranges.Should().BeEmpty();
        _repositoryMock.Verify(r => r.AddDepositRanges(It.IsAny<IEnumerable<TrustLevelDepositRange>>()), Times.Never);
    }

    [Fact]
    public async Task CreateLevelAsync_WhenNameAlreadyExists_ThrowsConflict()
    {
        _repositoryMock.Setup(r => r.LevelNameExistsAsync("Silver", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new CreateTrustLevelRequest
        {
            Name = "Silver",
            Requirement = new UpdateTrustLevelRequirementRequest(),
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        };

        var act = () => _service.CreateLevelAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("A trust level with this name already exists.");
    }

    [Fact]
    public async Task CreateLevelAsync_WhenRequirementsDoNotExceedActivePreviousLevel_ThrowsBadRequest()
    {
        var activeLow = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Bronze",
            Rank = 1,
            IsActive = true,
            Requirement = new TrustLevelRequirement { MinScore = 0, MinCompletedOrders = 0, MinTotalOrderValue = 0m }
        };
        var activeHigh = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Silver",
            Rank = 2,
            IsActive = true,
            Requirement = new TrustLevelRequirement { MinScore = 80, MinCompletedOrders = 5, MinTotalOrderValue = 500000m }
        };

        _repositoryMock.Setup(r => r.LevelNameExistsAsync("Gold", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.GetAllLevelsWithRequirementsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activeLow, activeHigh });

        var request = new CreateTrustLevelRequest
        {
            Name = "Gold",
            Requirement = new UpdateTrustLevelRequirementRequest { MinScore = 80, MinCompletedOrders = 5, MinTotalOrderValue = 500000m },
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        };

        var act = () => _service.CreateLevelAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Requirements must increase with the level rank");
    }

    [Fact]
    public async Task CreateLevelAsync_WhenPrecedingHighestLevelIsInactiveAndNotExceeded_ThrowsBadRequest()
    {
        var activeLow = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Bronze",
            Rank = 1,
            IsActive = true,
            Requirement = new TrustLevelRequirement { MinScore = 0, MinCompletedOrders = 0, MinTotalOrderValue = 0m }
        };
        var inactiveHighest = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Platinum",
            Rank = 2,
            IsActive = false,
            Requirement = new TrustLevelRequirement { MinScore = 95, MinCompletedOrders = 10, MinTotalOrderValue = 1000000m }
        };

        _repositoryMock.Setup(r => r.LevelNameExistsAsync("Diamond", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.GetAllLevelsWithRequirementsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { activeLow, inactiveHighest });

        var request = new CreateTrustLevelRequest
        {
            Name = "Diamond",
            Requirement = new UpdateTrustLevelRequirementRequest { MinScore = 90, MinCompletedOrders = 10, MinTotalOrderValue = 1000000m },
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        };

        var act = () => _service.CreateLevelAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Requirements must exceed the preceding level (Platinum, Rank 2)");
    }

    [Fact]
    public void CreateTrustLevelRequestValidator_ValidatesNameAndNestedFields()
    {
        var invalid = new CreateTrustLevelRequest
        {
            Name = " ",
            Requirement = new UpdateTrustLevelRequirementRequest { MinScore = 0 },
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        };

        var valid = new CreateTrustLevelRequest
        {
            Name = "Diamond",
            Requirement = new UpdateTrustLevelRequirementRequest { MinScore = 100, MinCompletedOrders = 10, MinTotalOrderValue = 1000000m },
            Benefits = new UpdateTrustLevelBenefitsRequest { RequireFullOnlinePayment = true }
        };

        _validator.Validate(invalid).IsValid.Should().BeFalse();
        _validator.Validate(valid).IsValid.Should().BeTrue();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
