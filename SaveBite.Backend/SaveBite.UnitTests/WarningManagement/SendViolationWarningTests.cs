using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.WarningManagement;

/// <summary>
/// Unit Tests for Feature: Send Violation Warning
/// Service: WarningService.IssueWarningAsync
/// Function Code: UT-MOD-04
/// </summary>
public class SendViolationWarningTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IWarningRepository> _warningRepoMock = new();
    private readonly AppDbContext _db;
    private readonly WarningService _service;

    private readonly Guid _adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public SendViolationWarningTests(ITestOutputHelper output)
    {
        _output = output;
        _db = TestDbContextFactory.Create();

        _service = new WarningService(
            _warningRepoMock.Object,
            _db,
            TimeProvider.System,
            new Mock<INotificationQueue>().Object,
            new Mock<IAdminAuditWriter>().Object
        );
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task UTCID01_IssueWarning_ValidProductWithPendingRevision_CreatesWarningAndUpdatesRevision()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            ShopId = shopId,
            Name = "Inaccurate Nutrition Drink",
            DeletedAt = null
        };

        var revision = new ProductContentRevision
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            RevisionNumber = 1,
            Status = ProductReviewStatus.Pending
        };

        var request = new IssueViolationWarningRequest
        {
            Reason = "Inaccurate sugar contents on packaging",
            RequiredCorrection = "Update nutritional table and specify exact sugar grams per 100ml"
        };

        _warningRepoMock
            .Setup(r => r.GetProductForWarningAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _warningRepoMock
            .Setup(r => r.GetLatestRevisionForWarningAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revision);

        // Act
        var result = await _service.IssueWarningAsync(_adminId, productId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TargetId.Should().Be(shopId);
        result.Action.Should().Be("SHOP_WARNING_ISSUED");

        revision.Status.Should().Be(ProductReviewStatus.NeedsRevision);
        revision.ReviewedByUserId.Should().Be(_adminId);
        revision.ReviewNote.Should().Be(request.RequiredCorrection);

        var createdWarning = await _db.ProductWarnings.FirstOrDefaultAsync(w => w.ProductId == productId);
        createdWarning.Should().NotBeNull();
        createdWarning!.Reason.Should().Be(request.Reason);
        createdWarning.RequiredCorrection.Should().Be(request.RequiredCorrection);
        createdWarning.ShopId.Should().Be(shopId);
        createdWarning.DueAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task UTCID02_IssueWarning_WhenProductNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new IssueViolationWarningRequest
        {
            Reason = "Reason",
            RequiredCorrection = "Correction"
        };

        _warningRepoMock
            .Setup(r => r.GetProductForWarningAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = () => _service.IssueWarningAsync(_adminId, productId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*Product not found*");
    }

    [Fact]
    public async Task UTCID03_IssueWarning_WhenProductHasUnresolvedWarning_ThrowsConflictException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var product = new Product { Id = productId, ShopId = shopId, Name = "Test Product" };
        var revision = new ProductContentRevision { Id = Guid.NewGuid(), ProductId = productId, Status = ProductReviewStatus.Pending };

        _db.ProductWarnings.Add(new ProductWarning
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ShopId = shopId,
            BaselineRevisionId = revision.Id,
            IssuedByUserId = _adminId,
            ProductName = product.Name,
            Reason = "Unresolved reason",
            RequiredCorrection = "Fix",
            ResolvedAt = null,
            DueAt = DateTime.UtcNow.AddDays(3)
        });
        await _db.SaveChangesAsync();

        _warningRepoMock
            .Setup(r => r.GetProductForWarningAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _warningRepoMock
            .Setup(r => r.GetLatestRevisionForWarningAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revision);

        var request = new IssueViolationWarningRequest { Reason = "New reason", RequiredCorrection = "New correction" };

        // Act
        var act = () => _service.IssueWarningAsync(_adminId, productId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .WithMessage("*This product already has an unresolved warning*");
    }
}
