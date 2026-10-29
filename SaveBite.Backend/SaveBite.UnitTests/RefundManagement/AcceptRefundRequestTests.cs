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
/// Unit Tests for Feature: Accept Refund Request (Store Owner)
/// Service: RefundService.ReviewAsync (approve: true)
/// Use Case: UC-105 (Accept Refund Request)
/// Business Rules: BR-121, BR-122, BR-123, BR-177, BR-196, BR-205
/// Sheet Name: UT_AcceptRefundRequest
/// </summary>
public class AcceptRefundRequestTests
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

    public AcceptRefundRequestTests(ITestOutputHelper output)
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

    private Refund CreateRefund(
        Guid refundId,
        Guid orderId,
        Guid shopId,
        RefundStatus status = RefundStatus.PendingVerification,
        RefundType type = RefundType.CustomerRequest,
        DateTime? destinationConfirmedAt = null,
        DateTime? reviewDeadlineAt = null,
        decimal paymentAmount = 150000m,
        decimal requestedAmount = 150000m)
    {
        var order = new Order
        {
            Id = orderId,
            OrderCode = "ORD-20261029-001",
            CustomerId = ValidCustomerId,
            ShopId = shopId,
            Status = OrderStatus.Completed,
            TotalAmount = paymentAmount,
            Shop = new Shop { Id = shopId, Name = "SaveBite Bakery", LogoUrl = "https://savebite.com/logos/shop.png" },
            Customer = new User { Id = ValidCustomerId, FullName = "Nguyen Van A", Email = "customer@savebite.com", AvatarUrl = "https://savebite.com/avatars/user.png" },
            Payments = new List<Payment>
            {
                new() { Id = Guid.NewGuid(), OrderId = orderId, Amount = paymentAmount, Status = PaymentStatus.Succeeded, PaymentType = PaymentType.Full }
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
            TransferredAmount = 0m,
            Reason = "Food quality issue",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            DestinationConfirmedAt = destinationConfirmedAt ?? DateTime.UtcNow.AddMinutes(-30),
            ReviewDeadlineAt = reviewDeadlineAt ?? DateTime.UtcNow.AddHours(10),
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
    /// TCID01 - Normal: Store owner approves valid CustomerRequest refund in PendingVerification status.
    /// Expects: 200 OK, Status transitions to AwaitingTransfer, ReviewedBy and ReviewedAt recorded, fee waived.
    /// BR-121, BR-122, BR-123, BR-205
    /// </summary>
    [Fact]
    public async Task TCID01_ReviewAsync_ApproveValidPendingRefund_TransitionsToAwaitingTransfer_Normal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _refundRepoMock.Setup(r => r.FeeAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync((OrderPlatformFee?)null);

        // Act
        var result = await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: true, reason: null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.AwaitingTransfer));
        refund.Status.Should().Be(RefundStatus.AwaitingTransfer);
        refund.ReviewedBy.Should().Be(StoreOwnerId);
        refund.ReviewedAt.Should().NotBeNull();
        refund.History.Should().ContainSingle(h => h.Decision == "Approved" && h.ToStatus == RefundStatus.AwaitingTransfer);

        _orderRepoMock.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"[TCID01] Success: Refund approved. New Status: {result.Status}, ReviewedBy: {refund.ReviewedBy}");
    }

    /// <summary>
    /// TCID02 - Normal: Repeated approval of already approved refund (AwaitingTransfer).
    /// Expects: 200 OK without re-auditing or state modification (Idempotency).
    /// </summary>
    [Fact]
    public async Task TCID02_ReviewAsync_AlreadyApproved_ReturnsExistingStatus_Normal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.AwaitingTransfer
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: true, reason: null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.AwaitingTransfer));
        refund.History.Should().BeEmpty(); // No duplicate history entry

        _output.WriteLine($"[TCID02] Success: Idempotent approval returned existing status {result.Status}.");
    }

    /// <summary>
    /// TCID03 - Abnormal: Refund does not exist.
    /// Expects: 404 Not Found ("Refund not found.").
    /// </summary>
    [Fact]
    public async Task TCID03_ReviewAsync_RefundNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _refundRepoMock.Setup(r => r.GetAsync(NonExistentRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        // Act
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, NonExistentRefundId, approve: true, reason: null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found.");

        _output.WriteLine($"[TCID03] Passed: Non-existent refund correctly threw 404 NotFound. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID04 - Abnormal: User is not authorized as the store owner (Non-Owner).
    /// Expects: 403 Forbidden ("Only the Store Owner may access refund management.").
    /// BR-177
    /// </summary>
    [Fact]
    public async Task TCID04_ReviewAsync_NonOwnerUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(NonOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var act = async () => await _service.ReviewAsync(NonOwnerId, ValidShopId, ValidRefundId, approve: true, reason: null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Only the Store Owner may access refund management.");

        _output.WriteLine($"[TCID04] Passed: Non-owner user correctly threw 403 Forbidden. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID05 - Abnormal: Ineligible refund type (ShopCancellation cannot be reviewed).
    /// Expects: 409 Conflict ("Only post-completion customer requests can be reviewed.").
    /// </summary>
    [Fact]
    public async Task TCID05_ReviewAsync_ShopCancellationRefund_ThrowsConflict_Abnormal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.PendingVerification,
            type: RefundType.ShopCancellation
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: true, reason: null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only post-completion customer requests can be reviewed.");

        _output.WriteLine($"[TCID05] Passed: ShopCancellation refund correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID06 - Abnormal: Refund is not in PendingVerification status (e.g. AwaitingCustomerDetails).
    /// Expects: 409 Conflict ("Review requires PendingVerification and a confirmed bank destination.").
    /// BR-121
    /// </summary>
    [Fact]
    public async Task TCID06_ReviewAsync_StatusNotPendingVerification_ThrowsConflict_Abnormal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.AwaitingCustomerDetails
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: true, reason: null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Review requires PendingVerification and a confirmed bank destination.");

        _output.WriteLine($"[TCID06] Passed: Non-PendingVerification status correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID07 - Abnormal: Shop review deadline expired (ReviewDeadlineAt <= Now).
    /// Expects: 409 Conflict ("The 24-hour shop processing deadline has passed. This refund can no longer be processed.").
    /// BR-196
    /// </summary>
    [Fact]
    public async Task TCID07_ReviewAsync_ReviewDeadlineExpired_ThrowsConflict_Abnormal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.PendingVerification,
            reviewDeadlineAt: DateTime.UtcNow.AddMinutes(-5) // Expired
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: true, reason: null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The 24-hour shop processing deadline has passed. This refund can no longer be processed.");

        _output.WriteLine($"[TCID07] Passed: Expired deadline correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID08 - Abnormal: Full eligible refundable amount changed from requested amount.
    /// Expects: 409 Conflict ("The full refundable amount changed; refresh the refund request.").
    /// BR-122
    /// </summary>
    [Fact]
    public async Task TCID08_ReviewAsync_EligibleAmountChanged_ThrowsConflict_Abnormal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.PendingVerification,
            paymentAmount: 100000m, // Eligible amount is 100k
            requestedAmount: 150000m // Requested is 150k -> mismatch!
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: true, reason: null, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The full refundable amount changed; refresh the refund request.");

        _output.WriteLine($"[TCID08] Passed: Changed eligible amount correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }
}
