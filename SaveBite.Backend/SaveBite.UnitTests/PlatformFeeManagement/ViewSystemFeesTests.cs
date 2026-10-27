using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.PlatformFeeManagement;

/// <summary>
/// Unit Tests for Feature: View System Fees
/// Service: PlatformFeeStatementService.GetShopStatementsAsync
/// Function Code: UT-104
/// Sheet Name: UT_ViewSystemFees
/// </summary>
public class ViewSystemFeesTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformFeeStatementRepository> _repositoryMock = new();
    private readonly Mock<ILogger<PlatformFeeStatementService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly PlatformFeeStatementService _service;

    // Explicit GUIDs for test determinism and traceability
    public static readonly Guid ValidShopId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OwnerUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid NonOwnerUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid NonExistentShopId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid StatementId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid StatementId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public ViewSystemFeesTests(ITestOutputHelper output)
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

    private Shop CreateValidShop() => new()
    {
        Id = ValidShopId,
        OwnerUserId = OwnerUserId,
        Name = "SaveBite Official Bakery",
        Status = ShopStatus.Active
    };

    private PlatformFeeStatement CreateStatement(
        Guid id,
        int year,
        short month,
        PlatformFeeStatementStatus status,
        decimal amount = 250000m) => new()
    {
        Id = id,
        ShopId = ValidShopId,
        BillingYear = year,
        BillingMonth = month,
        Status = status,
        TotalFeeAmount = amount,
        IssuedAt = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc),
        DueAt = status == PlatformFeeStatementStatus.Paid ? null : new DateTime(year, month, 8, 0, 0, 0, DateTimeKind.Utc),
        PaidAt = status == PlatformFeeStatementStatus.Paid ? new DateTime(year, month, 5, 0, 0, 0, DateTimeKind.Utc) : null,
        CreatedAt = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // =========================================================================
    // TCID01: Valid Store Owner, Default Filter -> Returns Statements List (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetShopStatements_ValidStoreOwner_ReturnsStatementsList_Normal()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new GetShopPlatformFeesRequest { Page = 1, PageSize = 10 };
        var statements = new List<PlatformFeeStatement>
        {
            CreateStatement(StatementId1, 2026, 10, PlatformFeeStatementStatus.Issued, 300000m),
            CreateStatement(StatementId2, 2026, 9, PlatformFeeStatementStatus.Paid, 250000m)
        };
        var pagedStatements = PagedResult<PlatformFeeStatement>.Create(statements, 1, 10, 2);

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetShopStatementsAsync(ValidShopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedStatements);

        // Act
        var result = await _service.GetShopStatementsAsync(ValidShopId, OwnerUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items[0].Id.Should().Be(StatementId1);
        result.Items[0].Status.Should().Be(PlatformFeeStatementStatus.Issued);
        result.Items[0].TotalFeeAmount.Should().Be(300000m);
        result.Items[1].Id.Should().Be(StatementId2);
        result.Items[1].Status.Should().Be(PlatformFeeStatementStatus.Paid);

        _repositoryMock.Verify(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.GetShopStatementsAsync(ValidShopId, request, It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Retrieved {result.Items.Count} statements successfully for Shop {ValidShopId}.");
    }

    // =========================================================================
    // TCID02: Non-Existent Shop -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetShopStatements_NonExistentShop_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new GetShopPlatformFeesRequest();

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(NonExistentShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act
        var act = () => _service.GetShopStatementsAsync(NonExistentShopId, OwnerUserId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Contain("Shop not found");

        _repositoryMock.Verify(r => r.GetShopStatementsAsync(It.IsAny<Guid>(), It.IsAny<GetShopPlatformFeesRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID02 PASSED] 404 NotFound thrown for non-existent Shop {NonExistentShopId}: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID03: Non-Owner User Access -> Throws 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetShopStatements_NonOwnerUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new GetShopPlatformFeesRequest();

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        // Act
        var act = () => _service.GetShopStatementsAsync(ValidShopId, NonOwnerUserId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Contain("Only the Store Owner may access the platform-fee statements");

        _repositoryMock.Verify(r => r.GetShopStatementsAsync(It.IsAny<Guid>(), It.IsAny<GetShopPlatformFeesRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID03 PASSED] 403 Forbidden thrown for non-owner User {NonOwnerUserId}: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: No statements exist for Shop -> Returns Empty List (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID04_GetShopStatements_NoStatements_ReturnsEmptyList_Boundary()
    {
        // Arrange
        var shop = CreateValidShop();
        var request = new GetShopPlatformFeesRequest { Page = 1, PageSize = 10 };
        var pagedStatements = PagedResult<PlatformFeeStatement>.Create(new List<PlatformFeeStatement>(), 1, 10, 0);

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetShopStatementsAsync(ValidShopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedStatements);

        // Act
        var result = await _service.GetShopStatementsAsync(ValidShopId, OwnerUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);

        _repositoryMock.Verify(r => r.GetShopStatementsAsync(ValidShopId, request, It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID04 PASSED] 200 OK - Successfully returned empty statement list with 0 total items for Shop {ValidShopId}.");
    }
}
