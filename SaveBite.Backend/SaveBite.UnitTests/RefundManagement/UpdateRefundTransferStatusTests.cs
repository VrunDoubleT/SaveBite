using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.RefundManagement;

/// <summary>
/// Unit Tests for Feature: Update Refund Transfer Status (Store Owner)
/// Service: RefundService.TransferAsync
/// Use Case: UC-107 (Update Refund Transfer Status)
/// Business Rules: BR-126, BR-177, BR-196, BR-203, BR-204, BR-205
/// Sheet Name: UT_UpdateRefundTransferStatus
/// </summary>
public class UpdateRefundTransferStatusTests
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

    public UpdateRefundTransferStatusTests(ITestOutputHelper output)
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

        // Passthrough order lock for unit testing
        _orderRepoMock
            .Setup(o => o.WithOrderLockAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<RefundResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns<Guid, Func<Task<RefundResponse>>, CancellationToken>((_, func, _) => func());
    }

    private User CreateStoreOwnerUser(Guid id)
    {
        return new User
        {
            Id = id,
            FullName = "Tran Van Chu Shop",
            Email = "owner@savebite.com",
            Role = UserRole.StoreOwner,
            Status = UserStatus.Active
        };
    }

    private RefundTransferRequest CreateValidRequest(string reference = "REF-20261029-001", DateTime? transferredAt = null)
    {
        return new RefundTransferRequest
        {
            TransferReference = reference,
            TransferredAt = transferredAt ?? DateTime.UtcNow.AddMinutes(-5),
            ProofUrl = "https://savebite.com/proofs/transfer.png"
        };
    }

    private Refund CreateRefund(
        Guid refundId,
        Guid orderId,
        Guid shopId,
        RefundStatus status = RefundStatus.AwaitingTransfer,
        RefundType type = RefundType.CustomerRequest,
        DateTime? destinationConfirmedAt = null,
        DateTime? reviewDeadlineAt = null,
        string? transferReference = null,
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
            ReviewedAt = DateTime.UtcNow.AddMinutes(-20),
            TransferReference = transferReference,
            ReceiverBankBin = "970422",
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
    /// TCID01 - Normal: Store owner records manual transfer for refund in AwaitingTransfer status.
    /// Expects: 200 OK, Status becomes Transferred, TransferredAmount updated, fee waived, accumulated value reclaimed.
    /// BR-126, BR-203, BR-204, BR-205
    /// </summary>
    [Fact]
    public async Task TCID01_TransferAsync_ValidTransfer_TransitionsToTransferred_Normal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);
        var request = CreateValidRequest();

        var trustLevelId = Guid.NewGuid();
        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _refundRepoMock.Setup(r => r.FeeAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync((OrderPlatformFee?)null);
        _refundRepoMock.Setup(r => r.ProcessingAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(new TrustOrderProcessing { CreditedOrderValue = 150000m });
        _trustRepoMock.Setup(t => t.ProgressAsync(ValidCustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserLevelProgress { AccumulatedOrderValue = 300000m, CompletedOrdersCount = 2 });
        _trustRepoMock.Setup(t => t.ScoreAsync(ValidCustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(new UserTrustScore { CurrentScore = 100, TrustLevelId = trustLevelId });
        _trustRepoMock.Setup(t => t.LevelsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<TrustLevel>
        {
            new() { Id = trustLevelId, Name = "Silver", Rank = 1, Requirement = new TrustLevelRequirement { MinScore = 50, MinCompletedOrders = 1, MinTotalOrderValue = 100000m } }
        });

        // Act
        var result = await _service.TransferAsync(StoreOwnerId, ValidShopId, ValidRefundId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.Transferred));
        refund.Status.Should().Be(RefundStatus.Transferred);
        refund.TransferredAmount.Should().Be(150000m);
        refund.TransferReference.Should().Be("REF-20261029-001");
        refund.AccumulatedValueReclaimedAt.Should().NotBeNull();
        refund.History.Should().ContainSingle(h => h.Decision == "Transferred" && h.ToStatus == RefundStatus.Transferred);

        _orderRepoMock.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"[TCID01] Success: Transfer recorded. New Status: {result.Status}, TransferredAmount: {refund.TransferredAmount}");
    }

    /// <summary>
    /// TCID02 - Normal: Repeated transfer call with same reference on already Transferred refund.
    /// Expects: 200 OK without modifying state or re-auditing (Idempotency).
    /// </summary>
    [Fact]
    public async Task TCID02_TransferAsync_AlreadyTransferred_ReturnsExistingStatus_Normal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.Transferred,
            transferReference: "REF-20261029-001",
            transferredAmount: 150000m
        );
        var request = CreateValidRequest();

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.TransferAsync(StoreOwnerId, ValidShopId, ValidRefundId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.Transferred));
        refund.History.Should().BeEmpty(); // No duplicate history entry

        _output.WriteLine($"[TCID02] Success: Idempotent transfer returned existing status {result.Status}.");
    }

    /// <summary>
    /// TCID03 - Abnormal: Transfer reference is missing / empty.
    /// Expects: 400 Bad Request ("A transfer reference is required.").
    /// BR-126
    /// </summary>
    [Fact]
    public async Task TCID03_TransferAsync_EmptyTransferReference_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new RefundTransferRequest
        {
            TransferReference = "   ", // Empty/whitespace
            ProofUrl = "https://savebite.com/proofs/transfer.png"
        };

        // Act
        var act = async () => await _service.TransferAsync(StoreOwnerId, ValidShopId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("A transfer reference is required.");

        _output.WriteLine($"[TCID03] Passed: Empty transfer reference correctly threw 400 BadRequest. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID04 - Abnormal: Refund does not exist.
    /// Expects: 404 Not Found ("Refund not found.").
    /// </summary>
    [Fact]
    public async Task TCID04_TransferAsync_RefundNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = CreateValidRequest();
        _refundRepoMock.Setup(r => r.GetAsync(NonExistentRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        // Act
        var act = async () => await _service.TransferAsync(StoreOwnerId, ValidShopId, NonExistentRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found.");

        _output.WriteLine($"[TCID04] Passed: Non-existent refund correctly threw 404 NotFound. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID05 - Abnormal: User is not authorized as the store owner (Non-Owner).
    /// Expects: 403 Forbidden ("Only the Store Owner may access refund management.").
    /// BR-177
    /// </summary>
    [Fact]
    public async Task TCID05_TransferAsync_NonOwnerUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);
        var request = CreateValidRequest();

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(NonOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var act = async () => await _service.TransferAsync(NonOwnerId, ValidShopId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Only the Store Owner may access refund management.");

        _output.WriteLine($"[TCID05] Passed: Non-owner user correctly threw 403 Forbidden. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID06 - Abnormal: Refund is not in AwaitingTransfer status (e.g. PendingVerification).
    /// Expects: 409 Conflict ("Transfer requires an approved refund with an email-confirmed bank destination.").
    /// BR-126
    /// </summary>
    [Fact]
    public async Task TCID06_TransferAsync_StatusNotAwaitingTransfer_ThrowsConflict_Abnormal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.PendingVerification
        );
        var request = CreateValidRequest();

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.TransferAsync(StoreOwnerId, ValidShopId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Transfer requires an approved refund with an email-confirmed bank destination.");

        _output.WriteLine($"[TCID06] Passed: Non-AwaitingTransfer status correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID07 - Abnormal: Processing deadline expired (ReviewDeadlineAt <= Now).
    /// Expects: 409 Conflict ("The 24-hour shop processing deadline has passed. This refund can no longer be processed.").
    /// BR-196
    /// </summary>
    [Fact]
    public async Task TCID07_TransferAsync_DeadlineExpired_ThrowsConflict_Abnormal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.AwaitingTransfer,
            reviewDeadlineAt: DateTime.UtcNow.AddMinutes(-5) // Expired
        );
        var request = CreateValidRequest(transferredAt: DateTime.UtcNow.AddMinutes(-10));

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.TransferAsync(StoreOwnerId, ValidShopId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The 24-hour shop processing deadline has passed. This refund can no longer be processed.");

        _output.WriteLine($"[TCID07] Passed: Expired deadline correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID08 - Abnormal: Transfer timestamp is in the future.
    /// Expects: 400 Bad Request ("Supply the actual UTC transfer time after confirmation and approval, not a future time.").
    /// </summary>
    [Fact]
    public async Task TCID08_TransferAsync_FutureTransferTimestamp_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);
        var request = CreateValidRequest(transferredAt: DateTime.UtcNow.AddHours(2)); // Future time

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.TransferAsync(StoreOwnerId, ValidShopId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Supply the actual UTC transfer time after confirmation and approval, not a future time.");

        _output.WriteLine($"[TCID08] Passed: Future transfer time correctly threw 400 BadRequest. Message: {ex.Which.Message}");
    }
}
