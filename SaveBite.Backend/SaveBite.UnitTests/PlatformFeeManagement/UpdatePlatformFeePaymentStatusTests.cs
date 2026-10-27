using System.Net;
using System.Text.Json;
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
/// Unit Tests for Feature: Update Platform Fee Payment Status
/// Service: PlatformFeeStatementService.ProcessFeeWebhookAsync
/// Function Code: UT-107
/// Sheet Name: UT_UpdatePlatformFeePaymentStatus
/// </summary>
public class UpdatePlatformFeePaymentStatusTests
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
    public static readonly Guid StatementId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid PaymentId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public const long OrderCode = 123456789;

    public UpdatePlatformFeePaymentStatusTests(ITestOutputHelper output)
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

    private PlatformFeeStatement CreateStatement(PlatformFeeStatementStatus status = PlatformFeeStatementStatus.Issued) => new()
    {
        Id = StatementId,
        ShopId = ValidShopId,
        BillingYear = 2026,
        BillingMonth = 10,
        Status = status,
        TotalFeeAmount = 500000m,
        IssuedAt = DateTime.UtcNow.AddDays(-5),
        DueAt = DateTime.UtcNow.AddDays(2),
        PaidAt = null,
        CreatedAt = DateTime.UtcNow.AddDays(-5),
        UpdatedAt = DateTime.UtcNow.AddDays(-5)
    };

    private PlatformFeePayment CreatePayment(
        Shop shop,
        PlatformFeeStatement statement,
        PaymentStatus status = PaymentStatus.Pending,
        decimal amount = 500000m) => new()
    {
        Id = PaymentId,
        StatementId = statement.Id,
        Statement = statement,
        ShopId = shop.Id,
        Shop = shop,
        Amount = amount,
        PaymentMethod = PaymentMethod.OnlinePayment,
        GatewayTransactionId = OrderCode.ToString(),
        Status = status,
        CreatedAt = DateTime.UtcNow.AddDays(-1),
        UpdatedAt = DateTime.UtcNow.AddDays(-1)
    };

    private static JsonElement BuildWebhookPayload(
        long orderCode,
        decimal amount,
        string code = "00")
    {
        var json = $$"""
        {
            "code": "{{code}}",
            "desc": "success",
            "data": {
                "orderCode": {{orderCode}},
                "amount": {{amount}},
                "description": "FEE1234567",
                "accountNumber": "123456",
                "reference": "REF123",
                "transactionDateTime": "2026-10-27 12:00:00",
                "currency": "VND",
                "paymentLinkId": "link123"
            }
        }
        """;
        return JsonDocument.Parse(json).RootElement;
    }

    // =========================================================================
    // TCID01: Successful payment & no other overdue -> Statement Paid & Shop Reactivated (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_ProcessFeeWebhook_FullSettlement_StatementPaidAndShopReactivated_Normal()
    {
        // Arrange
        var shop = CreateShop(ShopStatus.Suspended);
        var statement = CreateStatement(PlatformFeeStatementStatus.Issued);
        var payment = CreatePayment(shop, statement, PaymentStatus.Pending, 500000m);
        var webhook = BuildWebhookPayload(OrderCode, 500000m, "00");

        _repositoryMock
            .Setup(r => r.GetPaymentByGatewayCodeAsync(OrderCode.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _repositoryMock
            .Setup(r => r.CancelPendingPaymentsForStatementAsync(StatementId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.HasOtherOverdueStatementsAsync(ValidShopId, StatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // No other overdue

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessFeeWebhookAsync(webhook);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.PaidAt.Should().NotBeNull();
        statement.Status.Should().Be(PlatformFeeStatementStatus.Paid);
        statement.PaidAt.Should().NotBeNull();
        shop.Status.Should().Be(ShopStatus.Active); // Reactivated according to BR-195

        _repositoryMock.Verify(r => r.CancelPendingPaymentsForStatementAsync(StatementId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID01 PASSED] 200 OK - Statement {StatementId} marked Paid. Shop {ValidShopId} reactivated to Active.");
    }

    // =========================================================================
    // TCID02: Successful payment but other overdue remain -> Statement Paid & Shop Stays Suspended (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_ProcessFeeWebhook_HasOtherOverdue_StatementPaidAndShopStaysSuspended_Normal()
    {
        // Arrange
        var shop = CreateShop(ShopStatus.Suspended);
        var statement = CreateStatement(PlatformFeeStatementStatus.Issued);
        var payment = CreatePayment(shop, statement, PaymentStatus.Pending, 500000m);
        var webhook = BuildWebhookPayload(OrderCode, 500000m, "00");

        _repositoryMock
            .Setup(r => r.GetPaymentByGatewayCodeAsync(OrderCode.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _repositoryMock
            .Setup(r => r.CancelPendingPaymentsForStatementAsync(StatementId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.HasOtherOverdueStatementsAsync(ValidShopId, StatementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Still has other overdue statements

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessFeeWebhookAsync(webhook);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Succeeded);
        statement.Status.Should().Be(PlatformFeeStatementStatus.Paid);
        shop.Status.Should().Be(ShopStatus.Suspended); // Remains suspended according to BR-195

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[TCID02 PASSED] 200 OK - Statement {StatementId} marked Paid. Shop {ValidShopId} remains Suspended due to other overdue statements.");
    }

    // =========================================================================
    // TCID03: Underpayment (Amount < Statement Amount) -> Throws 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_ProcessFeeWebhook_Underpayment_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var shop = CreateShop();
        var statement = CreateStatement();
        var payment = CreatePayment(shop, statement, PaymentStatus.Pending, 500000m);
        var webhook = BuildWebhookPayload(OrderCode, 300000m, "00"); // Only paid 300k instead of 500k

        _repositoryMock
            .Setup(r => r.GetPaymentByGatewayCodeAsync(OrderCode.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act
        var act = () => _service.ProcessFeeWebhookAsync(webhook);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Payment amount does not match statement fee");

        payment.Status.Should().Be(PaymentStatus.Pending); // Not updated
        statement.Status.Should().Be(PlatformFeeStatementStatus.Issued);

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID03 PASSED] 400 BadRequest thrown for underpayment: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID04: Malformed Webhook Payload -> Throws 400 BadRequest (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_ProcessFeeWebhook_MalformedPayload_ThrowsBadRequest_Abnormal()
    {
        // Arrange: missing orderCode property
        var malformedJson = """
        {
            "code": "00",
            "desc": "success",
            "data": {
                "amount": 500000
            }
        }
        """;
        var webhook = JsonDocument.Parse(malformedJson).RootElement;

        // Act
        var act = () => _service.ProcessFeeWebhookAsync(webhook);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Contain("Invalid payment notification");

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID04 PASSED] 400 BadRequest thrown for malformed payload: {ex.Which.Message}");
    }

    // =========================================================================
    // TCID05: Idempotency (Payment already Succeeded) -> Returns without modifying (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID05_ProcessFeeWebhook_AlreadySucceeded_IdempotentNoOp_Normal()
    {
        // Arrange
        var shop = CreateShop(ShopStatus.Active);
        var statement = CreateStatement(PlatformFeeStatementStatus.Paid);
        var payment = CreatePayment(shop, statement, PaymentStatus.Succeeded, 500000m);
        var webhook = BuildWebhookPayload(OrderCode, 500000m, "00");

        _repositoryMock
            .Setup(r => r.GetPaymentByGatewayCodeAsync(OrderCode.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act
        await _service.ProcessFeeWebhookAsync(webhook);

        // Assert: No repository write operations
        _repositoryMock.Verify(r => r.CancelPendingPaymentsForStatementAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine($"[TCID05 PASSED] 200 OK - Duplicate webhook ignored idempotently for payment {PaymentId}.");
    }

    // =========================================================================
    // TCID06: Webhook Reported Unsuccessful Status Code (code != "00") -> No-Op (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task TCID06_ProcessFeeWebhook_UnsuccessfulCode_NoOp_Boundary()
    {
        // Arrange
        var webhook = BuildWebhookPayload(OrderCode, 500000m, code: "01"); // Payment failed or cancelled

        // Act
        await _service.ProcessFeeWebhookAsync(webhook);

        // Assert: should exit early without touching repository
        _repositoryMock.Verify(r => r.GetPaymentByGatewayCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        _output.WriteLine("[TCID06 PASSED] 200 OK - Unsuccessful webhook code '01' skipped without changes.");
    }
}
