using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.RefundManagement;

/// <summary>
/// Unit Tests for Feature: Generate VietQR Code (Store Owner)
/// Service: RefundService.GenerateQrAsync
/// Use Case: UC-108 (Generate VietQR Code)
/// Business Rules: BR-127, BR-128, BR-177, BR-178, BR-179
/// Sheet Name: UT_GenerateVietQRCode
/// </summary>
public class GenerateVietQRCodeTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IRefundRepository> _refundRepoMock = new();
    private readonly Mock<ICheckoutRepository> _orderRepoMock = new();
    private readonly Mock<IShopOrderRepository> _shopRepoMock = new();
    private readonly Mock<ITrustOrderRepository> _trustRepoMock = new();
    private readonly Mock<IEmailService> _emailMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<IRefundDeadlineScheduler> _deadlinesMock = new();
    private readonly Mock<ILogger<RefundService>> _loggerMock = new();
    private readonly RefundService _service;

    // Explicit GUIDs for test determinism and traceability
    public static readonly Guid StoreOwnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid NonOwnerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid ValidShopId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid ValidCustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidOrderId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid ValidRefundId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid NonExistentRefundId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public GenerateVietQRCodeTests(ITestOutputHelper output)
    {
        _output = output;
        _configMock.Setup(c => c["Jwt:SigningKey"]).Returns("SaveBite_Super_Secret_Unit_Testing_Key_2026_Secure!");

        _service = new RefundService(
            _refundRepoMock.Object,
            _orderRepoMock.Object,
            _shopRepoMock.Object,
            _trustRepoMock.Object,
            _emailMock.Object,
            TimeProvider.System,
            _configMock.Object,
            _deadlinesMock.Object,
            _loggerMock.Object
        );
    }

    private Refund CreateRefund(
        Guid refundId,
        Guid orderId,
        Guid shopId,
        RefundStatus status = RefundStatus.AwaitingTransfer,
        RefundType type = RefundType.CustomerRequest,
        DateTime? destinationConfirmedAt = null,
        DateTime? reviewDeadlineAt = null,
        string? bankBin = "970422",
        decimal requestedAmount = 150000m,
        decimal transferredAmount = 0m)
    {
        var order = new Order
        {
            Id = orderId,
            OrderCode = "ORD-20261029-001",
            CustomerId = ValidCustomerId,
            ShopId = shopId,
            Status = OrderStatus.Completed,
            TotalAmount = requestedAmount,
            Shop = new Shop { Id = shopId, Name = "SaveBite Bakery", LogoUrl = "https://savebite.com/logos/shop.png" },
            Customer = new User { Id = ValidCustomerId, FullName = "Nguyen Van A", Email = "customer@savebite.com", AvatarUrl = "https://savebite.com/avatars/user.png" },
            Payments = new List<Payment>
            {
                new() { Id = Guid.NewGuid(), OrderId = orderId, Amount = requestedAmount, Status = PaymentStatus.Succeeded, PaymentType = PaymentType.Full }
            },
            Refunds = new List<Refund>()
        };

        var refund = new Refund
        {
            Id = refundId,
            OrderId = orderId,
            RefundType = type,
            Status = status,
            RequestedAmount = requestedAmount,
            TransferredAmount = transferredAmount,
            Reason = "Food quality issue",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            DestinationConfirmedAt = destinationConfirmedAt ?? DateTime.UtcNow.AddMinutes(-30),
            ReviewDeadlineAt = reviewDeadlineAt ?? DateTime.UtcNow.AddHours(10),
            ReceiverBankBin = bankBin,
            ReceiverBankName = "MBBank",
            ReceiverAccountNumber = "0123456789",
            ReceiverAccountHolder = "NGUYEN VAN A",
            Order = order,
            History = new List<RefundStatusHistory>(),
            EvidenceImages = new List<RefundEvidenceImage>()
        };

        order.Refunds.Add(refund);
        return refund;
    }

    /// <summary>
    /// TCID01 - Normal: Store owner generates VietQR for valid AwaitingTransfer refund with confirmed destination.
    /// Expects: 200 OK, returns RefundQrResponse with full approved amount, bank info, transfer content, and QR image URL.
    /// BR-127, BR-128
    /// </summary>
    [Fact]
    public async Task TCID01_GenerateQrAsync_ValidAwaitingTransfer_ReturnsVietQr_Normal()
    {
        // Arrange
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, false, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.GenerateQrAsync(StoreOwnerId, ValidShopId, ValidRefundId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RefundId.Should().Be(ValidRefundId);
        result.Amount.Should().Be(150000m);
        result.BankBin.Should().Be("970422");
        result.AccountNumber.Should().Be("0123456789");
        result.AccountHolder.Should().Be("NGUYEN VAN A");
        result.TransferContent.Should().StartWith("SB RF ");
        result.ImageUrl.Should().Contain("img.vietqr.io/image/970422-0123456789-compact2.png");

        _output.WriteLine($"[TCID01] Success: VietQR generated. Amount={result.Amount}, Content={result.TransferContent}, URL={result.ImageUrl}");
    }

    /// <summary>
    /// TCID02 - Abnormal: Refund does not exist.
    /// Expects: 404 Not Found ("Refund not found.").
    /// </summary>
    [Fact]
    public async Task TCID02_GenerateQrAsync_RefundNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _refundRepoMock.Setup(r => r.GetAsync(NonExistentRefundId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        // Act
        var act = async () => await _service.GenerateQrAsync(StoreOwnerId, ValidShopId, NonExistentRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found.");

        _output.WriteLine($"[TCID02] Passed: Non-existent refund correctly threw 404 NotFound. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID03 - Abnormal: User is not authorized as the store owner (Non-Owner).
    /// Expects: 403 Forbidden ("Only the Store Owner may access refund management.").
    /// BR-177
    /// </summary>
    [Fact]
    public async Task TCID03_GenerateQrAsync_NonOwnerUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, false, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(NonOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var act = async () => await _service.GenerateQrAsync(NonOwnerId, ValidShopId, ValidRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Only the Store Owner may access refund management.");

        _output.WriteLine($"[TCID03] Passed: Non-owner user correctly threw 403 Forbidden. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID04 - Abnormal: Refund is not in AwaitingTransfer status (e.g. PendingVerification).
    /// Expects: 409 Conflict ("Transfer requires an approved refund with an email-confirmed bank destination.").
    /// BR-127, BR-178, BR-179
    /// </summary>
    [Fact]
    public async Task TCID04_GenerateQrAsync_StatusNotAwaitingTransfer_ThrowsConflict_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.PendingVerification
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, false, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.GenerateQrAsync(StoreOwnerId, ValidShopId, ValidRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Transfer requires an approved refund with an email-confirmed bank destination.");

        _output.WriteLine($"[TCID04] Passed: Non-AwaitingTransfer status correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID05 - Abnormal: Processing deadline expired (ReviewDeadlineAt <= Now).
    /// Expects: 409 Conflict ("The 24-hour shop processing deadline has passed. This refund can no longer be processed.").
    /// </summary>
    [Fact]
    public async Task TCID05_GenerateQrAsync_DeadlineExpired_ThrowsConflict_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.AwaitingTransfer,
            reviewDeadlineAt: DateTime.UtcNow.AddMinutes(-5) // Expired
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, false, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.GenerateQrAsync(StoreOwnerId, ValidShopId, ValidRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The 24-hour shop processing deadline has passed. This refund can no longer be processed.");

        _output.WriteLine($"[TCID05] Passed: Expired deadline correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID06 - Abnormal: Bank destination has missing / null Bank BIN.
    /// Expects: 400 Bad Request ("Supply a valid bank BIN, bank name, account number and account holder.").
    /// </summary>
    [Fact]
    public async Task TCID06_GenerateQrAsync_MissingBankBin_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.AwaitingTransfer,
            bankBin: null // Missing BIN
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, false, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.GenerateQrAsync(StoreOwnerId, ValidShopId, ValidRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Supply a valid bank BIN, bank name, account number and account holder.");

        _output.WriteLine($"[TCID06] Passed: Missing Bank BIN correctly threw 400 BadRequest. Message: {ex.Which.Message}");
    }
}
