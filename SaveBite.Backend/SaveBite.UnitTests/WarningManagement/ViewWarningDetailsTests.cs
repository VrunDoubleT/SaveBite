using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.WarningManagement;

/// <summary>
/// Unit Tests for Feature: View Warning Details
/// Service: WarningService.GetDetailsAsync
/// Function Code: UT-MOD-09
/// </summary>
public class ViewWarningDetailsTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IWarningRepository> _warningRepoMock = new();
    private readonly AppDbContext _db;
    private readonly WarningService _service;

    private readonly Guid _ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ViewWarningDetailsTests(ITestOutputHelper output)
    {
        _output = output;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _service = new WarningService(_warningRepoMock.Object, _db, TimeProvider.System);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task UTCID01_GetDetails_ExistingWarning_ReturnsWarningDetails()
    {
        // Arrange
        var warningId = Guid.NewGuid();
        _warningRepoMock
            .Setup(r => r.GetOwnedShopIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_shopId);

        var warning = new ProductWarning
        {
            Id = warningId,
            ProductId = Guid.NewGuid(),
            ProductName = "Dairy Cake",
            ShopId = _shopId,
            Reason = "Lack of allergen warning for dairy",
            RequiredCorrection = "Add allergy notice on packaging",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            DueAt = DateTime.UtcNow.AddDays(6)
        };

        _warningRepoMock
            .Setup(r => r.GetByIdAsync(_shopId, warningId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warning);

        // Act
        var result = await _service.GetDetailsAsync(_ownerId, warningId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(warningId);
        result.ProductName.Should().Be("Dairy Cake");
        result.Reason.Should().Be("Lack of allergen warning for dairy");
        result.RequiredCorrection.Should().Be("Add allergy notice on packaging");
    }

    [Fact]
    public async Task UTCID02_GetDetails_EmptyGuid_ThrowsBadRequestException()
    {
        // Act
        var act = () => _service.GetDetailsAsync(_ownerId, Guid.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Warning ID is invalid*");
    }

    [Fact]
    public async Task UTCID03_GetDetails_WarningNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var warningId = Guid.NewGuid();
        _warningRepoMock
            .Setup(r => r.GetOwnedShopIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_shopId);

        _warningRepoMock
            .Setup(r => r.GetByIdAsync(_shopId, warningId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductWarning?)null);

        // Act
        var act = () => _service.GetDetailsAsync(_ownerId, warningId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Warning not found*");
    }
}
