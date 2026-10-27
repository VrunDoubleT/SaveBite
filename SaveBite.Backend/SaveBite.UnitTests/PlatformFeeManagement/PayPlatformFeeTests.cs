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
/// Unit Tests for Feature: Pay Platform Fee
/// Service: PlatformFeeStatementService.CreatePaymentLinkAsync
/// Function Code: UT-106
/// Sheet Name: UT_PayPlatformFee
/// </summary>
public class PayPlatformFeeTests
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
    public static readonly Guid IssuedStatementId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid OverdueStatementId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid PaidStatementId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid ZeroAmountStatementId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public static readonly Guid NonExistentStatementId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public PayPlatformFeeTests(ITestOutputHelper output)
    {
        _output = output;
        _configurationMock.Setup(c => c["Checkout:PaymentProvider"]).Returns("Simulated");

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
        Guid statementId,
        PlatformFeeStatementStatus status,
        decimal amount = 450000m,
        DateTime? dueAt = null) => new()
    {
        Id = statementId,
        ShopId = ValidShopId,
        BillingYear = 2026,
        BillingMonth = 10,
        Status = status,
        TotalFeeAmount = amount,
        IssuedAt = DateTime.UtcNow.AddDays(-3),
        DueAt = dueAt ?? DateTime.UtcNow.AddDays(4),
        PaidAt = status == PlatformFeeStatementStatus.Paid ? DateTime.UtcNow.AddDays(-1) : null,
        CreatedAt = DateTime.UtcNow.AddDays(-3),
        UpdatedAt = DateTime.UtcNow.AddDays(-3)
    };

    // =========================================================================
    // TCID01: Issued Statement -> Returns 200 OK & Payment Link (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_CreatePaymentLink_IssuedStatement_ReturnsPaymentLink_Normal()
    {
        // Arrange
        var shop = CreateValidShop();
        var statement = CreateStatement(IssuedStatementId, PlatformFeeStatementStatus.Issued, 450000m);

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(IssuedStatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        _repositoryMock
            .Setup(r => r.CancelPendingPaymentsForStatementAsync(IssuedStatementId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.AddPaymentAsync(It.IsAny<PlatformFeePayment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePaymentLinkAsync(IssuedStatementId, ValidShopId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result.StatementId.Should().Be(IssuedStatementId);
        result.Amount.Should().Be(450000m);
        result.CheckoutUrl.Should().NotBeNullOrWhiteSpace();
        result.QrCode.Should().NotBeNullOrWhiteSpace();
        result.OrderCode.Should().BeGreaterThan(0);

        _repositoryMock.Verify(r => r.CancelPendingPaymentsForStatementAsync(IssuedStatementId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddPaymentAsync(It.Is<PlatformFeePayment>(p =>
            p.StatementId == IssuedStatementId &&
            p.ShopId == ValidShopId &&
            p.Amount == 450000m &&
            p.Status == PaymentStatus.Pending), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Payment Link created: {result.CheckoutUrl}, Amount: {result.Amount}");
    }

    // =========================================================================
    // TCID02: Overdue Statement -> Returns 200 OK & Payment Link with 24h grace (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_CreatePaymentLink_OverdueStatement_ReturnsPaymentLink_Normal()
    {
        // Arrange
        var shop = CreateValidShop();
        var statement = CreateStatement(OverdueStatementId, PlatformFeeStatementStatus.Overdue, 600000m, dueAt: DateTime.UtcNow.AddDays(-2));

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(OverdueStatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        _repositoryMock
            .Setup(r => r.CancelPendingPaymentsForStatementAsync(OverdueStatementId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.AddPaymentAsync(It.IsAny<PlatformFeePayment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreatePaymentLinkAsync(OverdueStatementId, ValidShopId, OwnerUserId);

        // Assert
        result.Should().NotBeNull();
        result.StatementId.Should().Be(OverdueStatementId);
        result.Amount.Should().Be(600000m);
        result.CheckoutUrl.Should().NotBeNullOrWhiteSpace();
        result.ExpiredAt.Should().BeAfter(DateTime.UtcNow);

        _repositoryMock.Verify(r => r.AddPaymentAsync(It.Is<PlatformFeePayment>(p =>
            p.StatementId == OverdueStatementId &&
            p.Amount == 600000m &&
            p.Status == PaymentStatus.Pending), It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID02 PASSED] 200 OK - Overdue statement payment link created with grace expiry: {result.ExpiredAt}");
    }

    // =========================================================================
    // TCID03: Paid Statement -> Throws 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_CreatePaymentLink_PaidStatement_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var shop = CreateValidShop();
        var statement = CreateStatement(PaidStatementId, PlatformFeeStatementStatus.Paid, 450000m);

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(PaidStatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        // Act
        var act = () => _service.CreatePaymentLinkAsync(PaidStatementId, ValidShopId, OwnerUserId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Only an Issued or Overdue fee statement can enter in-app payment");

        _repositoryMock.Verify(r => r.AddPaymentAsync(It.IsAny<PlatformFeePayment>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID03 PASSED] 400 BadRequest thrown for Paid statement {PaidStatementId}: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: Statement Not Found -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_CreatePaymentLink_StatementNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var shop = CreateValidShop();

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(NonExistentStatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeeStatement?)null);

        // Act
        var act = () => _service.CreatePaymentLinkAsync(NonExistentStatementId, ValidShopId, OwnerUserId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Contain("Platform fee statement not found");

        _repositoryMock.Verify(r => r.AddPaymentAsync(It.IsAny<PlatformFeePayment>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID04 PASSED] 404 NotFound thrown for non-existent Statement {NonExistentStatementId}: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID05: Unauthorized Non-Owner User -> Throws 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_CreatePaymentLink_NonOwnerUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var shop = CreateValidShop();

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        // Act
        var act = () => _service.CreatePaymentLinkAsync(IssuedStatementId, ValidShopId, NonOwnerUserId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Contain("Only the Store Owner may access the platform-fee statements");

        _repositoryMock.Verify(r => r.AddPaymentAsync(It.IsAny<PlatformFeePayment>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID05 PASSED] 403 Forbidden thrown for non-owner User {NonOwnerUserId}: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID06: Zero Amount Payable -> Throws 400 BadRequest (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID06_CreatePaymentLink_ZeroAmount_ThrowsBadRequest_Boundary()
    {
        // Arrange
        var shop = CreateValidShop();
        var statement = CreateStatement(ZeroAmountStatementId, PlatformFeeStatementStatus.Issued, amount: 0m);

        _repositoryMock
            .Setup(r => r.GetShopByIdAsync(ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(ZeroAmountStatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        // Act
        var act = () => _service.CreatePaymentLinkAsync(ZeroAmountStatementId, ValidShopId, OwnerUserId);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("The amount payable must be greater than 0");

        _repositoryMock.Verify(r => r.AddPaymentAsync(It.IsAny<PlatformFeePayment>(), It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID06 PASSED] 400 BadRequest thrown for zero payable amount on Statement {ZeroAmountStatementId}: {ex.Which.Message}");
    }
}
