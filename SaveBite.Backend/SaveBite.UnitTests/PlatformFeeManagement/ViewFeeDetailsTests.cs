using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.PlatformFeeManagement;

/// <summary>
/// Unit Tests for Feature: View Fee Details
/// Service: PlatformFeeStatementService.GetStatementDetailAsync
/// Function Code: UT-105
/// Sheet Name: UT_ViewFeeDetails
/// </summary>
public class ViewFeeDetailsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformFeeStatementRepository> _repositoryMock = new();
    private readonly Mock<ILogger<PlatformFeeStatementService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly PlatformFeeStatementService _service;

    // Explicit GUIDs for test determinism and traceability
    public static readonly Guid ValidShopId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OtherShopId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid OwnerUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid NonOwnerUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid StatementId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid OtherShopStatementId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid NonExistentStatementId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid OrderId1 = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid RefundId1 = Guid.Parse("66666666-6666-6666-6666-666666666666");

    public ViewFeeDetailsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new PlatformFeeStatementService(
            _repositoryMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            _configurationMock.Object,
            _httpClientFactoryMock.Object
        );
    }

    private Shop CreateValidShop(Guid shopId, Guid ownerUserId, string name = "SaveBite Official Bakery") => new()
    {
        Id = shopId,
        OwnerUserId = ownerUserId,
        Name = name,
        Status = ShopStatus.Active
    };

    private PlatformFeeStatement CreateStatementDetail(Guid statementId, Guid shopId, Shop shop) => new()
    {
        Id = statementId,
        ShopId = shopId,
        Shop = shop,
        BillingYear = 2026,
        BillingMonth = 10,
        Status = PlatformFeeStatementStatus.Issued,
        TotalFeeAmount = 450000m,
        IssuedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        DueAt = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc),
        PaidAt = null,
        CreatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        OrderPlatformFees = new List<OrderPlatformFee>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StatementId = statementId,
                ShopId = shopId,
                OrderId = OrderId1,
                GrossAmount = 500000m,
                FeeRate = 0.10m,
                FeeAmount = 50000m,
                CalculatedAt = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc)
            }
        },
        Adjustments = new List<PlatformFeeAdjustment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StatementId = statementId,
                ShopId = shopId,
                RefundId = RefundId1,
                Amount = -5000m,
                Reason = "Refund adjustment for cancelled order item",
                CreatedAt = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc)
            }
        },
        Payments = new List<PlatformFeePayment>()
    };

    // =========================================================================
    // TCID01: Valid Store Owner views own statement -> Returns 200 OK (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetStatementDetail_ValidStoreOwner_ReturnsStatementDetail_Normal()
    {
        // Arrange
        var shop = CreateValidShop(ValidShopId, OwnerUserId);
        var statement = CreateStatementDetail(StatementId, ValidShopId, shop);

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(StatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        // Act
        var result = await _service.GetStatementDetailAsync(StatementId, ValidShopId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(StatementId);
        result.ShopId.Should().Be(ValidShopId);
        result.ShopName.Should().Be("SaveBite Official Bakery");
        result.BillingYear.Should().Be(2026);
        result.BillingMonth.Should().Be(10);
        result.Status.Should().Be(PlatformFeeStatementStatus.Issued);
        result.TotalFeeAmount.Should().Be(450000m);
        result.Orders.Should().HaveCount(1);
        result.Orders[0].OrderId.Should().Be(OrderId1);
        result.Adjustments.Should().HaveCount(1);
        result.Adjustments[0].RefundId.Should().Be(RefundId1);

        _repositoryMock.Verify(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.GetStatementDetailAsync(StatementId, It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Retrieved Statement {StatementId} with {result.Orders.Count} order items and {result.Adjustments.Count} adjustments.");
    }

    // =========================================================================
    // TCID02: Statement Not Found -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetStatementDetail_StatementNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var shop = CreateValidShop(ValidShopId, OwnerUserId);

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(NonExistentStatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeeStatement?)null);

        // Act
        var act = () => _service.GetStatementDetailAsync(NonExistentStatementId, ValidShopId, OwnerUserId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Contain("Platform fee statement not found");

        _output.WriteLine($"[TCID02 PASSED] 404 NotFound thrown for non-existent Statement {NonExistentStatementId}: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: Unauthorized Non-Owner User Access -> Throws 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetStatementDetail_NonOwnerUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var shop = CreateValidShop(ValidShopId, OwnerUserId);

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        // Act
        var act = () => _service.GetStatementDetailAsync(StatementId, ValidShopId, NonOwnerUserId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Contain("Only the Store Owner may access the platform-fee statements");

        _repositoryMock.Verify(r => r.GetStatementDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID03 PASSED] 403 Forbidden thrown for non-owner User {NonOwnerUserId}: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: Statement Belongs to Another Shop -> Throws 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_GetStatementDetail_StatementBelongsToOtherShop_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var shop = CreateValidShop(ValidShopId, OwnerUserId);
        var otherShop = CreateValidShop(OtherShopId, Guid.NewGuid(), "Other Shop");
        var otherStatement = CreateStatementDetail(OtherShopStatementId, OtherShopId, otherShop);

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(OtherShopStatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherStatement);

        // Act
        var act = () => _service.GetStatementDetailAsync(OtherShopStatementId, ValidShopId, OwnerUserId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Contain("This platform fee statement does not belong to your shop");

        _output.WriteLine($"[TCID04 PASSED] 403 Forbidden thrown when accessing Statement {OtherShopStatementId} belonging to Shop {OtherShopId}: {ex.Which.Message}");
    }
}
