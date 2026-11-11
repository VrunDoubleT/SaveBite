using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.WarningManagement;

/// <summary>
/// Unit Tests for Feature: View Warning History
/// Service: WarningService.GetHistoryAsync
/// Function Code: UT-MOD-08
/// </summary>
public class ViewWarningHistoryTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IWarningRepository> _warningRepoMock = new();
    private readonly AppDbContext _db;
    private readonly WarningService _service;

    private readonly Guid _ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ViewWarningHistoryTests(ITestOutputHelper output)
    {
        _output = output;
        _db = TestDbContextFactory.Create();

        _service = new WarningService(_warningRepoMock.Object, _db, TimeProvider.System, new Mock<INotificationQueue>().Object, new Mock<IAdminAuditWriter>().Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task UTCID01_GetHistory_WithValidFilter_ReturnsPagedWarnings()
    {
        // Arrange
        _warningRepoMock
            .Setup(r => r.GetOwnedShopIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_shopId);

        var warning = new ProductWarning
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductName = "Sample Product",
            RequiredCorrection = "Update label",
            ShopId = _shopId,
            Reason = "Label mismatch",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            DueAt = DateTime.UtcNow.AddDays(5)
        };

        _warningRepoMock
            .Setup(r => r.GetHistoryAsync(_shopId, 1, 10, It.IsAny<CancellationToken>(), "new"))
            .ReturnsAsync((new List<ProductWarning> { warning }, 1));

        // Act
        var result = await _service.GetHistoryAsync(_ownerId, 1, 10, CancellationToken.None, "new");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductName.Should().Be("Sample Product");
        result.Items.First().RequiredCorrection.Should().Be("Update label");
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task UTCID02_GetHistory_InvalidStatusFilter_ThrowsBadRequestException()
    {
        // Act
        var act = () => _service.GetHistoryAsync(_ownerId, 1, 10, CancellationToken.None, "invalid_status");

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Invalid warning status filter*");
    }

    [Fact]
    public async Task UTCID03_GetHistory_OwnerHasNoShop_ThrowsNotFoundException()
    {
        // Arrange
        _warningRepoMock
            .Setup(r => r.GetOwnedShopIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act
        var act = () => _service.GetHistoryAsync(_ownerId, 1, 10, CancellationToken.None, "all");

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Shop not found*");
    }
}
