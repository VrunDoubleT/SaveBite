using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.TrustScore;

/// <summary>
/// Unit Tests for Feature: Update Level Benefits (UC-130)
/// Service: TrustConfigService.UpdateLevelBenefitsAsync & UpdateTrustLevelBenefitsRequestValidator
/// Function Code: UT_UpdateLevelBenefits
/// Sheet Name: UT_UpdateLevelBenefits
/// Business Rules: BR-050, BR-173
/// </summary>
public class UpdateLevelBenefitsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITrustConfigRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TrustConfigService>> _loggerMock = new();
    private readonly TrustConfigService _service;
    private readonly UpdateTrustLevelBenefitsRequestValidator _validator = new();

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _levelId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _nonExistentLevelId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public UpdateLevelBenefitsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new TrustConfigService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    private TrustLevel CreateSampleLevel(string? description = "Old benefit text: 5% discount on flash deals.", bool requireFullOnline = true)
    {
        var level = new TrustLevel
        {
            Id = _levelId,
            Name = "Silver Member",
            Description = description,
            Rank = 2,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            Requirement = new TrustLevelRequirement
            {
                Id = Guid.NewGuid(),
                TrustLevelId = _levelId,
                MinScore = 100,
                MinCompletedOrders = 5,
                MinTotalOrderValue = 200000m
            }
        };

        level.DepositPolicy = new TrustLevelDepositPolicy
        {
            Id = Guid.NewGuid(),
            TrustLevelId = _levelId,
            RequireFullOnlinePayment = requireFullOnline,
            DepositFreeOrderAmount = null,
            MaxDepositOrderAmount = null,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };

        return level;
    }

    // =========================================================================
    // UTCID01: Update level benefit description with valid text -> Success (Normal - N)
    // Business Rules: BR-050, BR-173
    // =========================================================================
    [Fact]
    public async Task UTCID01_UpdateLevelBenefits_ValidText_UpdatesSuccessfully_Normal()
    {
        // Arrange
        var level = CreateSampleLevel();
        var request = new UpdateTrustLevelBenefitsRequest
        {
            Description = "Updated benefit text: 10% discount on rescue packs, 3 concurrent orders.",
            RequireFullOnlinePayment = true
        };

        _repositoryMock
            .Setup(r => r.GetLevelForUpdateAsync(_levelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(level);

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { _levelId, 25 } });

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateLevelBenefitsAsync(_adminId, _levelId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_levelId);
        result.Description.Should().Be("Updated benefit text: 10% discount on rescue packs, 3 concurrent orders.");
        result.UserCount.Should().Be(25);

        _repositoryMock.Verify(r => r.AddAuditLog(It.Is<AuditLog>(log =>
            log.ActorUserId == _adminId &&
            log.Action == "UpdateTrustLevelBenefits" &&
            log.TargetId == _levelId
        )), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Clear level benefit description (set to null or empty) -> Description becomes null (Normal - N)
    // Business Rules: BR-050, BR-173
    // =========================================================================
    [Fact]
    public async Task UTCID02_UpdateLevelBenefits_EmptyDescription_ClearsToNull_Normal()
    {
        // Arrange
        var level = CreateSampleLevel("Existing benefit text");
        var request = new UpdateTrustLevelBenefitsRequest
        {
            Description = "   ", // Whitespace should be trimmed to null
            RequireFullOnlinePayment = true
        };

        _repositoryMock
            .Setup(r => r.GetLevelForUpdateAsync(_levelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(level);

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { _levelId, 10 } });

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateLevelBenefitsAsync(_adminId, _levelId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.Description.Should().BeNull();

        _repositoryMock.Verify(r => r.AddAuditLog(It.Is<AuditLog>(log =>
            log.ActorUserId == _adminId &&
            log.Action == "UpdateTrustLevelBenefits" &&
            log.TargetId == _levelId
        )), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Update with identical description -> Skips audit log & save changes (Normal - N)
    // Business Rules: BR-173
    // =========================================================================
    [Fact]
    public async Task UTCID03_UpdateLevelBenefits_IdenticalDescription_SkipsAuditAndSave_Normal()
    {
        // Arrange
        const string currentDescription = "Silver tier: standard benefits.";
        var level = CreateSampleLevel(currentDescription);
        var request = new UpdateTrustLevelBenefitsRequest
        {
            Description = currentDescription,
            RequireFullOnlinePayment = true
        };

        _repositoryMock
            .Setup(r => r.GetLevelForUpdateAsync(_levelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(level);

        _repositoryMock
            .Setup(r => r.GetUserCountsByLevelAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { _levelId, 15 } });

        // Act
        var result = await _service.UpdateLevelBenefitsAsync(_adminId, _levelId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.Description.Should().Be(currentDescription);

        _repositoryMock.Verify(r => r.AddAuditLog(It.IsAny<AuditLog>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Level ID not found -> 404 Not Found (Abnormal - A)
    // Business Rules: BR-050
    // =========================================================================
    [Fact]
    public async Task UTCID04_UpdateLevelBenefits_LevelNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateTrustLevelBenefitsRequest
        {
            Description = "New benefit text"
        };

        _repositoryMock
            .Setup(r => r.GetLevelForUpdateAsync(_nonExistentLevelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrustLevel?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateLevelBenefitsAsync(_adminId, _nonExistentLevelId, request, CancellationToken.None);

        // Assert & Log
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be("Trust level not found.");
        PrintError("UTCID04", (int)exception.Which.StatusCode, exception.Which.Message);

        _repositoryMock.Verify(r => r.AddAuditLog(It.IsAny<AuditLog>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: Description exceeds 2000 characters -> Fails Validation 400 Bad Request (Boundary - B)
    // Business Rules: BR-050
    // =========================================================================
    [Fact]
    public void UTCID05_UpdateLevelBenefits_DescriptionTooLong_FailsValidation_Boundary()
    {
        // Arrange
        var request = new UpdateTrustLevelBenefitsRequest
        {
            Description = new string('A', 2001) // 2001 characters (> 2000)
        };

        // Act
        var validationResult = _validator.Validate(request);

        // Assert & Log
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(request.Description));
        PrintError("UTCID05", 400, validationResult.Errors.First(e => e.PropertyName == nameof(request.Description)).ErrorMessage);
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
