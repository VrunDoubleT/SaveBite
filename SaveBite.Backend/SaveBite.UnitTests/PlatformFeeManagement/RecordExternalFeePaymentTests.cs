using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.PlatformFeeManagement;

/// <summary>
/// Unit Tests for Feature: Record External Fee Payment
/// Service: PlatformFeeStatementService.RecordExternalPaymentAsync
/// Function Code: UT-108
/// Sheet Name: UT_RecordExternalFeePayment
/// </summary>
public class RecordExternalFeePaymentTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IPlatformFeeStatementRepository> _repositoryMock = new();
    private readonly Mock<ILogger<PlatformFeeStatementService>> _loggerMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly PlatformFeeStatementService _service;

    // Explicit GUIDs for test determinism and traceability
    public static readonly Guid AdminId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid ValidShopId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OwnerUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid StatementId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid PaidStatementId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid NonExistentStatementId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public const string ValidRefCode = "EXT-20261027-001";
    public const string DuplicateRefCode = "EXT-DUPLICATE-001";

    public RecordExternalFeePaymentTests(ITestOutputHelper output)
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

    private Shop CreateShop(ShopStatus status = ShopStatus.Suspended) => new()
    {
        Id = ValidShopId,
        OwnerUserId = OwnerUserId,
        Name = "SaveBite Official Bakery",
        Status = status
    };

    private PlatformFeeStatement CreateStatement(
        Guid id,
        Shop shop,
        PlatformFeeStatementStatus status = PlatformFeeStatementStatus.Issued,
        decimal amount = 500000m) => new()
    {
        Id = id,
        ShopId = shop.Id,
        Shop = shop,
        BillingYear = 2026,
        BillingMonth = 10,
        Status = status,
        TotalFeeAmount = amount,
        IssuedAt = DateTime.UtcNow.AddDays(-10),
        DueAt = DateTime.UtcNow.AddDays(-3),
        PaidAt = status == PlatformFeeStatementStatus.Paid ? DateTime.UtcNow.AddDays(-1) : null,
        CreatedAt = DateTime.UtcNow.AddDays(-10),
        UpdatedAt = DateTime.UtcNow.AddDays(-10)
    };

    // =========================================================================
    // TCID01: Full Payment & No other overdue -> Statement Paid & Shop Reactivated (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_RecordExternalPayment_FullPaymentNoOtherOverdue_StatementPaidShopReactivated_Normal()
    {
        // Arrange
        var shop = CreateShop(ShopStatus.Suspended);
        var statement = CreateStatement(StatementId, shop, PlatformFeeStatementStatus.Overdue, 500000m);
        var request = new RecordExternalFeePaymentRequest
        {
            Amount = 500000m,
            ReferenceCode = ValidRefCode,
            Note = "Bank transfer reconciliation verified"
        };

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(StatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        _repositoryMock
            .Setup(r => r.GetPaymentByGatewayCodeAsync(ValidRefCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeePayment?)null);

        _repositoryMock
            .Setup(r => r.CancelPendingPaymentsForStatementAsync(StatementId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.AddPaymentAsync(It.IsAny<PlatformFeePayment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.HasOtherOverdueStatementsAsync(ValidShopId, StatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // No other overdue statements

        _repositoryMock
            .Setup(r => r.AddAuditLog(It.IsAny<AuditLog>()));

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RecordExternalPaymentAsync(AdminId, StatementId, request);

        // Assert
        statement.Status.Should().Be(PlatformFeeStatementStatus.Paid);
        statement.PaidAt.Should().NotBeNull();
        shop.Status.Should().Be(ShopStatus.Active); // Reactivated according to BR-195

        _repositoryMock.Verify(r => r.AddPaymentAsync(It.Is<PlatformFeePayment>(p =>
            p.StatementId == StatementId &&
            p.Amount == 500000m &&
            p.PaymentMethod == PaymentMethod.PayAtShop &&
            p.GatewayTransactionId == ValidRefCode &&
            p.Status == PaymentStatus.Succeeded), It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r => r.AddAuditLog(It.Is<AuditLog>(a =>
            a.ActorUserId == AdminId &&
            a.Action == "RecordExternalPlatformFeePayment" &&
            a.TargetId == StatementId)), Times.Once);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Statement {StatementId} marked Paid, Shop {ValidShopId} reactivated to Active.");
    }

    // =========================================================================
    // TCID02: Full Payment but other overdue remain -> Statement Paid & Shop Stays Suspended (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_RecordExternalPayment_HasOtherOverdue_StatementPaidShopStaysSuspended_Normal()
    {
        // Arrange
        var shop = CreateShop(ShopStatus.Suspended);
        var statement = CreateStatement(StatementId, shop, PlatformFeeStatementStatus.Overdue, 500000m);
        var request = new RecordExternalFeePaymentRequest
        {
            Amount = 500000m,
            ReferenceCode = ValidRefCode,
            Note = "Partial reconciliation"
        };

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(StatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        _repositoryMock
            .Setup(r => r.GetPaymentByGatewayCodeAsync(ValidRefCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeePayment?)null);

        _repositoryMock
            .Setup(r => r.CancelPendingPaymentsForStatementAsync(StatementId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.AddPaymentAsync(It.IsAny<PlatformFeePayment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.HasOtherOverdueStatementsAsync(ValidShopId, StatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Still has other overdue statements

        _repositoryMock
            .Setup(r => r.AddAuditLog(It.IsAny<AuditLog>()));

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.RecordExternalPaymentAsync(AdminId, StatementId, request);

        // Assert
        statement.Status.Should().Be(PlatformFeeStatementStatus.Paid);
        shop.Status.Should().Be(ShopStatus.Suspended); // Remains suspended according to BR-195

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID02 PASSED] 200 OK - Statement {StatementId} marked Paid. Shop {ValidShopId} remains Suspended due to other overdue statements.");
    }

    // =========================================================================
    // TCID03: Unauthorized Non-Admin -> Throws 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_RecordExternalPayment_UnauthorizedNonAdmin_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var request = new RecordExternalFeePaymentRequest { Amount = 500000m };

        // Act
        var act = () => _service.RecordExternalPaymentAsync(Guid.Empty, StatementId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Contain("Only an Administrator may record external payment reconciliation");

        _repositoryMock.Verify(r => r.GetStatementDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID03 PASSED] 403 Forbidden thrown for non-admin: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: Zero or Negative Amount -> Throws 400 BadRequest (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID04_RecordExternalPayment_ZeroAmount_ThrowsBadRequest_Boundary()
    {
        // Arrange
        var request = new RecordExternalFeePaymentRequest { Amount = 0m };

        // Act
        var act = () => _service.RecordExternalPaymentAsync(AdminId, StatementId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Payment amount must be greater than zero");

        _repositoryMock.Verify(r => r.GetStatementDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID04 PASSED] 400 BadRequest thrown for zero amount: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID05: Statement Not Found -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_RecordExternalPayment_StatementNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new RecordExternalFeePaymentRequest { Amount = 500000m };

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(NonExistentStatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformFeeStatement?)null);

        // Act
        var act = () => _service.RecordExternalPaymentAsync(AdminId, NonExistentStatementId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Contain("Platform fee statement not found");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID05 PASSED] 404 NotFound thrown for non-existent statement: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID06: Statement Already Paid -> Throws 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID06_RecordExternalPayment_StatementAlreadyPaid_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var shop = CreateShop();
        var statement = CreateStatement(PaidStatementId, shop, PlatformFeeStatementStatus.Paid, 500000m);
        var request = new RecordExternalFeePaymentRequest { Amount = 500000m };

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(PaidStatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        // Act
        var act = () => _service.RecordExternalPaymentAsync(AdminId, PaidStatementId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("This platform fee statement is already paid and settled");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID06 PASSED] 400 BadRequest thrown for already paid statement: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID07: Underpayment (Amount < Statement Amount) -> Throws 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID07_RecordExternalPayment_Underpayment_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var shop = CreateShop();
        var statement = CreateStatement(StatementId, shop, PlatformFeeStatementStatus.Issued, 500000m);
        var request = new RecordExternalFeePaymentRequest { Amount = 300000m }; // Underpayment

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(StatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        // Act
        var act = () => _service.RecordExternalPaymentAsync(AdminId, StatementId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("cannot be less than total fee amount");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID07 PASSED] 400 BadRequest thrown for underpayment: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID08: Duplicate Reference Code -> Throws 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID08_RecordExternalPayment_DuplicateRefCode_ThrowsConflict_Abnormal()
    {
        // Arrange
        var shop = CreateShop();
        var statement = CreateStatement(StatementId, shop, PlatformFeeStatementStatus.Issued, 500000m);
        var request = new RecordExternalFeePaymentRequest
        {
            Amount = 500000m,
            ReferenceCode = DuplicateRefCode
        };
        var existingPayment = new PlatformFeePayment
        {
            Id = Guid.NewGuid(),
            GatewayTransactionId = DuplicateRefCode
        };

        _repositoryMock
            .Setup(r => r.GetStatementDetailAsync(StatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statement);

        _repositoryMock
            .Setup(r => r.GetPaymentByGatewayCodeAsync(DuplicateRefCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        // Act
        var act = () => _service.RecordExternalPaymentAsync(AdminId, StatementId, request);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Contain("Reconciliation reference code already exists");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID08 PASSED] 409 Conflict thrown for duplicate reference code: {ex.Which.Message}");
    }
}
