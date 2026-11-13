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

namespace SaveBite.UnitTests.TrustScore;

public class UpdateLevelStatusTests
{
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly Mock<INotificationQueue> _notificationMock = new();
    private readonly TrustConfigService _service;

    public UpdateLevelStatusTests()
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
            TimeProvider.System,
            _loggerMock.Object,
            _notificationMock.Object);
    }

    [Fact]
    public async Task UpdateLevelStatusAsync_WhenDeactivatingNonLowestActiveLevel_DeactivatesQueuesOutboxAndWritesHistory()
    {
        var adminId = Guid.NewGuid();
        var lowLevel = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Bronze",
            Rank = 1,
            IsActive = true,
            Requirement = new TrustLevelRequirement { MinScore = 0, MinCompletedOrders = 0, MinTotalOrderValue = 0m }
        };
        var highLevel = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Gold",
            Rank = 2,
            IsActive = true,
            Requirement = new TrustLevelRequirement { MinScore = 80, MinCompletedOrders = 5, MinTotalOrderValue = 500000m }
        };

        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(highLevel.Id, It.IsAny<CancellationToken>())).ReturnsAsync(highLevel);
        _repositoryMock.Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int> { [highLevel.Id] = 7 });
        _repositoryMock.Setup(r => r.GetActiveLevelsForValidationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { lowLevel, highLevel });
        _repositoryMock.Setup(r => r.GetNextLevelHistoryVersionAsync(highLevel.Id, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await _service.UpdateLevelStatusAsync(adminId, highLevel.Id, new UpdateTrustLevelStatusRequest { IsActive = false });

        result.IsActive.Should().BeFalse();
        highLevel.IsActive.Should().BeFalse();
        _repositoryMock.Verify(r => r.AddLevelHistory(It.Is<TrustLevelHistory>(h =>
            h.ChangeType == TrustLevelChangeType.Deactivated &&
            h.AffectedUserCount == 7 &&
            h.VersionNo == 2)), Times.Once);
        _repositoryMock.Verify(r => r.IncrementPolicyVersionAndQueueOutboxAsync("Level Gold deactivated", It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.Verify(a => a.Record(It.Is<AuditLog>(log => log.Action == AuditActionTypes.DeactivateTrustLevel)), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLevelStatusAsync_WhenStatusAlreadyMatches_ReturnsWithoutSaving()
    {
        var levelId = Guid.NewGuid();
        var level = new TrustLevel { Id = levelId, Name = "Bronze", Rank = 1, IsActive = true };

        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(levelId, It.IsAny<CancellationToken>())).ReturnsAsync(level);
        _repositoryMock.Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _service.UpdateLevelStatusAsync(Guid.NewGuid(), levelId, new UpdateTrustLevelStatusRequest { IsActive = true });

        result.IsActive.Should().BeTrue();
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLevelStatusAsync_WhenDeactivatingOnlyActiveLevel_ThrowsBadRequest()
    {
        var onlyLevel = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Bronze",
            Rank = 1,
            IsActive = true,
            Requirement = new TrustLevelRequirement()
        };

        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(onlyLevel.Id, It.IsAny<CancellationToken>())).ReturnsAsync(onlyLevel);
        _repositoryMock.Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());
        _repositoryMock.Setup(r => r.GetActiveLevelsForValidationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { onlyLevel });

        var act = () => _service.UpdateLevelStatusAsync(Guid.NewGuid(), onlyLevel.Id, new UpdateTrustLevelStatusRequest { IsActive = false });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Cannot deactivate the only active trust level.");
    }

    [Fact]
    public async Task UpdateLevelStatusAsync_WhenDeactivatingLowestActiveLevel_ThrowsBadRequest()
    {
        var low = new TrustLevel { Id = Guid.NewGuid(), Name = "Bronze", Rank = 1, IsActive = true, Requirement = new TrustLevelRequirement() };
        var high = new TrustLevel { Id = Guid.NewGuid(), Name = "Silver", Rank = 2, IsActive = true, Requirement = new TrustLevelRequirement { MinScore = 80 } };

        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(low.Id, It.IsAny<CancellationToken>())).ReturnsAsync(low);
        _repositoryMock.Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());
        _repositoryMock.Setup(r => r.GetActiveLevelsForValidationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { low, high });

        var act = () => _service.UpdateLevelStatusAsync(Guid.NewGuid(), low.Id, new UpdateTrustLevelStatusRequest { IsActive = false });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Cannot deactivate the lowest active level (Bronze)");
    }

    [Fact]
    public async Task UpdateLevelStatusAsync_WhenReactivatingValidLevel_ActivatesWritesHistoryAndQueuesOutbox()
    {
        var adminId = Guid.NewGuid();
        var low = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Bronze",
            Rank = 1,
            IsActive = true,
            Requirement = new TrustLevelRequirement { MinScore = 0, MinCompletedOrders = 0, MinTotalOrderValue = 0m }
        };
        var inactiveHigh = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Gold",
            Rank = 2,
            IsActive = false,
            Requirement = new TrustLevelRequirement { MinScore = 90, MinCompletedOrders = 3, MinTotalOrderValue = 200000m },
            DepositPolicy = new TrustLevelDepositPolicy { Id = Guid.NewGuid(), RequireFullOnlinePayment = true }
        };

        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(inactiveHigh.Id, It.IsAny<CancellationToken>())).ReturnsAsync(inactiveHigh);
        _repositoryMock.Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());
        _repositoryMock.Setup(r => r.GetActiveLevelsForValidationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { low });
        _repositoryMock.Setup(r => r.GetNextLevelHistoryVersionAsync(inactiveHigh.Id, It.IsAny<CancellationToken>())).ReturnsAsync(4);

        var result = await _service.UpdateLevelStatusAsync(adminId, inactiveHigh.Id, new UpdateTrustLevelStatusRequest { IsActive = true });

        result.IsActive.Should().BeTrue();
        inactiveHigh.IsActive.Should().BeTrue();
        _repositoryMock.Verify(r => r.AddLevelHistory(It.Is<TrustLevelHistory>(h =>
            h.ChangeType == TrustLevelChangeType.Reactivated &&
            h.VersionNo == 4 &&
            h.AffectedUserCount == 0)), Times.Once);
        _repositoryMock.Verify(r => r.IncrementPolicyVersionAndQueueOutboxAsync("Level Gold reactivated", It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.Verify(a => a.Record(It.Is<AuditLog>(log => log.Action == AuditActionTypes.ReactivateTrustLevel)), Times.Once);
    }

    [Theory]
    [InlineData(true, false, "Configure level requirements before activating this level.")]
    [InlineData(false, true, "Configure level deposit policy before activating this level.")]
    public async Task UpdateLevelStatusAsync_WhenReactivatingMissingRequirementOrPolicy_ThrowsBadRequest(
        bool missingRequirement, bool missingPolicy, string expectedMessage)
    {
        var level = new TrustLevel
        {
            Id = Guid.NewGuid(),
            Name = "Gold",
            Rank = 2,
            IsActive = false,
            Requirement = missingRequirement ? null : new TrustLevelRequirement { MinScore = 90 },
            DepositPolicy = missingPolicy ? null : new TrustLevelDepositPolicy { RequireFullOnlinePayment = true }
        };

        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(level.Id, It.IsAny<CancellationToken>())).ReturnsAsync(level);
        _repositoryMock.Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());
        _repositoryMock.Setup(r => r.GetActiveLevelsForValidationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TrustLevel>());

        var act = () => _service.UpdateLevelStatusAsync(Guid.NewGuid(), level.Id, new UpdateTrustLevelStatusRequest { IsActive = true });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task UpdateLevelStatusAsync_WhenLevelNotFound_ThrowsNotFound()
    {
        var levelId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(levelId, It.IsAny<CancellationToken>())).ReturnsAsync((TrustLevel?)null);

        var act = () => _service.UpdateLevelStatusAsync(Guid.NewGuid(), levelId, new UpdateTrustLevelStatusRequest { IsActive = false });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Trust level not found.");
    }
}
