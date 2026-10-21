using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.TrustScore;

/// <summary>
/// Unit Tests for Feature: Update Level Requirements (UC-132)
/// Service: TrustConfigService.UpdateLevelRequirementAsync & UpdateTrustLevelRequirementRequestValidator
/// Function Code: UT_UpdateLevelRequirements
/// Sheet Name: UT_UpdateLevelRequirements
/// Business Rules: BR-051, BR-157, BR-158, BR-173
/// </summary>
public class UpdateLevelRequirementsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly TrustConfigService _service;
    private readonly UpdateTrustLevelRequirementRequestValidator _validator = new();

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _level1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _level2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _nonExistentLevelId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public UpdateLevelRequirementsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustConfigService(
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
                MinScore = 100,
                MinCompletedOrders = 5,
                MinTotalOrderValue = 200000m
            }
        };

        return (level1, level2);
    }

    // =========================================================================
    // UTCID01: Update higher-ranked level with valid increasing thresholds -> Success (Normal - N)
    // Business Rules: BR-051, BR-157, BR-173
    // =========================================================================
    [Fact]
    public async Task UTCID01_UpdateLevelRequirement_ValidThresholds_Success_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var request = new UpdateTrustLevelRequirementRequest
        {
            MinScore = 150,
            MinCompletedOrders = 8,
            MinTotalOrderValue = 300000m
        };

        _repositoryMock
            .Setup(r => r.GetLevelForUpdateAsync(_level2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(level2);

        _repositoryMock
            .Setup(r => r.GetActiveLevelsForValidationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2 });

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { _level2Id, 12 } });

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateLevelRequirementAsync(_adminId, _level2Id, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_level2Id);
        result.Requirement.Should().NotBeNull();
        result.Requirement!.MinScore.Should().Be(150);
        result.Requirement.MinCompletedOrders.Should().Be(8);
        result.Requirement.MinTotalOrderValue.Should().Be(300000m);
        result.UserCount.Should().Be(12);

        _repositoryMock.Verify(r => r.AddAuditLog(It.Is<AuditLog>(log =>
            log.ActorUserId == _adminId &&
            log.Action == "UpdateTrustLevelRequirement" &&
            log.TargetId == _level2Id
        )), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Update lowest active level with non-zero requirements -> 400 Bad Request (Abnormal - A)
    // Business Rules: BR-158
    // =========================================================================
    [Fact]
    public async Task UTCID02_UpdateLevelRequirement_LowestLevelNonZero_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var request = new UpdateTrustLevelRequirementRequest
        {
            MinScore = 50,
            MinCompletedOrders = 2,
            MinTotalOrderValue = 50000m
        };

        _repositoryMock
            .Setup(r => r.GetLevelForUpdateAsync(_level1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(level1);

        _repositoryMock
            .Setup(r => r.GetActiveLevelsForValidationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2 });

        // Act
        Func<Task> act = async () => await _service.UpdateLevelRequirementAsync(_adminId, _level1Id, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Contain("must not have any minimum requirement");
        PrintError("UTCID02", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddAuditLog(It.IsAny<AuditLog>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: Thresholds do not increase consistently with rank -> 400 Bad Request (Abnormal - A)
    // Business Rules: BR-157
    // =========================================================================
    [Fact]
    public async Task UTCID03_UpdateLevelRequirement_ThresholdNotIncreasing_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        // Rank 2 setting all to 0, which equals Rank 1 without any increase
        var request = new UpdateTrustLevelRequirementRequest
        {
            MinScore = 0,
            MinCompletedOrders = 0,
            MinTotalOrderValue = 0m
        };

        _repositoryMock
            .Setup(r => r.GetLevelForUpdateAsync(_level2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(level2);

        _repositoryMock
            .Setup(r => r.GetActiveLevelsForValidationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2 });

        // Act
        Func<Task> act = async () => await _service.UpdateLevelRequirementAsync(_adminId, _level2Id, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Contain("Requirements must increase with the level rank");
        PrintError("UTCID03", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddAuditLog(It.IsAny<AuditLog>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Level ID not found -> 404 Not Found (Abnormal - A)
    // Business Rules: BR-051
    // =========================================================================
    [Fact]
    public async Task UTCID04_UpdateLevelRequirement_LevelNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateTrustLevelRequirementRequest
        {
            MinScore = 150,
            MinCompletedOrders = 5,
            MinTotalOrderValue = 200000m
        };

        _repositoryMock
            .Setup(r => r.GetLevelForUpdateAsync(_nonExistentLevelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrustLevel?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateLevelRequirementAsync(_adminId, _nonExistentLevelId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Trust level not found.");
        PrintError("UTCID04", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddAuditLog(It.IsAny<AuditLog>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: Update with identical requirement values -> Success with no audit/save changes (Normal - N)
    // Business Rules: BR-051, BR-173
    // =========================================================================
    [Fact]
    public async Task UTCID05_UpdateLevelRequirement_IdenticalValues_SkipsAuditAndSave_Normal()
    {
        // Arrange
        var (level1, level2) = CreateSampleLevels();
        var request = new UpdateTrustLevelRequirementRequest
        {
            MinScore = 100,
            MinCompletedOrders = 5,
            MinTotalOrderValue = 200000m
        };

        _repositoryMock
            .Setup(r => r.GetLevelForUpdateAsync(_level2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(level2);

        _repositoryMock
            .Setup(r => r.GetActiveLevelsForValidationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrustLevel> { level1, level2 });

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { _level2Id, 12 } });

        // Act
        var result = await _service.UpdateLevelRequirementAsync(_adminId, _level2Id, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID05", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_level2Id);
        result.Requirement.Should().NotBeNull();
        result.Requirement!.MinScore.Should().Be(100);

        _repositoryMock.Verify(r => r.AddAuditLog(It.IsAny<AuditLog>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID06: Request with negative MinScore -> Validation Error 400 Bad Request (Boundary - B)
    // Business Rules: BR-051
    // =========================================================================
    [Fact]
    public void UTCID06_UpdateLevelRequirement_NegativeMinScore_FailsValidation_Boundary()
    {
        // Arrange
        var request = new UpdateTrustLevelRequirementRequest
        {
            MinScore = -1,
            MinCompletedOrders = 5,
            MinTotalOrderValue = 100000m
        };

        // Act
        var validationResult = _validator.Validate(request);

        // Assert & Log
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(request.MinScore));
        PrintError("UTCID06", 400, validationResult.Errors.First(e => e.PropertyName == nameof(request.MinScore)).ErrorMessage);
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

    private void PrintError(string tcid, int statusCode, string errorMessage)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS (Expected Exception Caught)");
        _output.WriteLine($"STATUS: {statusCode}");
        _output.WriteLine($"ERROR: {errorMessage}");
        _output.WriteLine("========================================");
    }
}
