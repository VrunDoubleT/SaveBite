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
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;

namespace SaveBite.UnitTests.TrustScore;

public class UpdateLevelInfoTests
{
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<IAdminAuditWriter> _auditMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly Mock<INotificationQueue> _notificationMock = new();
    private readonly TrustConfigService _service;
    private readonly UpdateTrustLevelInfoRequestValidator _validator = new();

    public UpdateLevelInfoTests()
    {
        _service = new TrustConfigService(
            _repositoryMock.Object,
            _auditMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            _notificationMock.Object);
    }

    [Fact]
    public async Task UpdateLevelInfoAsync_WhenNameChanges_UpdatesNameRecordsHistoryAuditAndSaves()
    {
        var adminId = Guid.NewGuid();
        var levelId = Guid.NewGuid();
        var level = new TrustLevel
        {
            Id = levelId,
            Name = "Silver",
            Rank = 2,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(levelId, It.IsAny<CancellationToken>())).ReturnsAsync(level);
        _repositoryMock.Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int> { [levelId] = 15 });
        _repositoryMock.Setup(r => r.LevelNameExistsAsync("Silver Plus", levelId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.GetNextLevelHistoryVersionAsync(levelId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await _service.UpdateLevelInfoAsync(adminId, levelId, new UpdateTrustLevelInfoRequest { Name = "  Silver Plus  " });

        result.Name.Should().Be("Silver Plus");
        result.UserCount.Should().Be(15);
        level.Name.Should().Be("Silver Plus");

        _repositoryMock.Verify(r => r.AddLevelHistory(It.Is<TrustLevelHistory>(h =>
            h.TrustLevelId == levelId &&
            h.VersionNo == 3 &&
            h.ChangeType == TrustLevelChangeType.InfoUpdated &&
            h.AffectedUserCount == 15 &&
            h.Name == "Silver Plus")), Times.Once);
        _auditMock.Verify(a => a.Record(It.Is<AuditLog>(log => log.Action == AuditActionTypes.UpdateTrustLevelInfo && log.TargetId == levelId)), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLevelInfoAsync_WhenNameUnchanged_ReturnsWithoutHistoryOrSave()
    {
        var levelId = Guid.NewGuid();
        var level = new TrustLevel { Id = levelId, Name = "Bronze", Rank = 1, IsActive = true };

        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(levelId, It.IsAny<CancellationToken>())).ReturnsAsync(level);
        _repositoryMock.Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());

        var result = await _service.UpdateLevelInfoAsync(Guid.NewGuid(), levelId, new UpdateTrustLevelInfoRequest { Name = "  Bronze " });

        result.Name.Should().Be("Bronze");
        _repositoryMock.Verify(r => r.AddLevelHistory(It.IsAny<TrustLevelHistory>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLevelInfoAsync_WhenLevelNotFound_ThrowsNotFound()
    {
        var levelId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(levelId, It.IsAny<CancellationToken>())).ReturnsAsync((TrustLevel?)null);

        var act = () => _service.UpdateLevelInfoAsync(Guid.NewGuid(), levelId, new UpdateTrustLevelInfoRequest { Name = "Gold" });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Trust level not found.");
    }

    [Fact]
    public async Task UpdateLevelInfoAsync_WhenDuplicateNameExists_ThrowsConflict()
    {
        var levelId = Guid.NewGuid();
        var level = new TrustLevel { Id = levelId, Name = "Silver", Rank = 2, IsActive = true };

        _repositoryMock.Setup(r => r.GetLevelForUpdateAsync(levelId, It.IsAny<CancellationToken>())).ReturnsAsync(level);
        _repositoryMock.Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<Guid, int>());
        _repositoryMock.Setup(r => r.LevelNameExistsAsync("Gold", levelId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _service.UpdateLevelInfoAsync(Guid.NewGuid(), levelId, new UpdateTrustLevelInfoRequest { Name = "Gold" });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Another trust level with this name already exists.");
    }

    [Fact]
    public async Task UpdateLevelInfoAsync_WhenNameBlank_ThrowsBadRequest()
    {
        var act = () => _service.UpdateLevelInfoAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateTrustLevelInfoRequest { Name = "   " });

        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Level name must be between 1 and 100 characters.");
    }

    [Fact]
    public void UpdateTrustLevelInfoRequestValidator_ValidatesEmptyAndLengthBounds()
    {
        _validator.Validate(new UpdateTrustLevelInfoRequest { Name = "" }).IsValid.Should().BeFalse();
        _validator.Validate(new UpdateTrustLevelInfoRequest { Name = "   " }).IsValid.Should().BeFalse();
        _validator.Validate(new UpdateTrustLevelInfoRequest { Name = new string('a', 101) }).IsValid.Should().BeFalse();
        _validator.Validate(new UpdateTrustLevelInfoRequest { Name = "Trusted Tier" }).IsValid.Should().BeTrue();
    }
}
