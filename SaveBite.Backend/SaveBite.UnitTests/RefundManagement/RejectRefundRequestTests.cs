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
/// Unit Tests for Feature: Reject Refund Request (Store Owner)
/// Service: RefundService.ReviewAsync (approve: false)
/// Use Case: UC-106 (Reject Refund Request)
/// Business Rules: BR-124, BR-125, BR-177, BR-196
/// Sheet Name: UT_RejectRefundRequest
/// </summary>
public class RejectRefundRequestTests
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

    public RejectRefundRequestTests(ITestOutputHelper output)
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
        string? rejectReason = null)
    {
        var order = new Order
        {
            Id = orderId,
            OrderCode = "ORD-20261029-001",
            CustomerId = ValidCustomerId,
            ShopId = shopId,
            Status = OrderStatus.Completed,
            TotalAmount = 150000m,
            Shop = new Shop { Id = shopId, Name = "SaveBite Bakery", LogoUrl = "https://savebite.com/logos/shop.png" },
            Customer = new User { Id = ValidCustomerId, FullName = "Nguyen Van A", Email = "customer@savebite.com", AvatarUrl = "https://savebite.com/avatars/user.png" },
            Payments = new List<Payment>
            {
                new() { Id = Guid.NewGuid(), OrderId = orderId, Amount = 150000m, Status = PaymentStatus.Succeeded, PaymentType = PaymentType.Full }
            },
            Refunds = new List<Refund>()
        };

        var refund = new Refund
        {
            Id = refundId,
            OrderId = orderId,
            RefundType = type,
            Status = status,
            RequestedAmount = 150000m,
            TransferredAmount = 0m,
            Reason = "Food quality issue",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            DestinationConfirmedAt = destinationConfirmedAt ?? DateTime.UtcNow.AddMinutes(-30),
            ReviewDeadlineAt = reviewDeadlineAt ?? DateTime.UtcNow.AddHours(10),
            RejectReason = rejectReason,
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
    /// TCID01 - Normal: Store owner rejects valid CustomerRequest refund in PendingVerification status with valid reason.
    /// Expects: 200 OK, Status transitions to Rejected, RejectReason recorded, audit history logged.
    /// BR-124, BR-125
    /// </summary>
    [Fact]
    public async Task TCID01_ReviewAsync_RejectValidPendingRefund_TransitionsToRejected_Normal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);
        const string reason = "Product meets quality standards";

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: false, reason: reason, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.Rejected));
        refund.Status.Should().Be(RefundStatus.Rejected);
        refund.RejectReason.Should().Be(reason);
        refund.ReviewedBy.Should().Be(StoreOwnerId);
        refund.ReviewedAt.Should().NotBeNull();
        refund.History.Should().ContainSingle(h => h.Decision == "Rejected" && h.ToStatus == RefundStatus.Rejected);

        _orderRepoMock.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"[TCID01] Success: Refund rejected. New Status: {result.Status}, Reason: {refund.RejectReason}");
    }

    /// <summary>
    /// TCID02 - Normal: Repeated rejection of already rejected refund with same reason.
    /// Expects: 200 OK without re-auditing or state modification (Idempotency).
    /// </summary>
    [Fact]
    public async Task TCID02_ReviewAsync_AlreadyRejected_ReturnsExistingStatus_Normal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        const string reason = "Product meets quality standards";
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.Rejected,
            rejectReason: reason
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: false, reason: reason, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.Rejected));
        refund.History.Should().BeEmpty(); // No duplicate history entry

        _output.WriteLine($"[TCID02] Success: Idempotent rejection returned existing status {result.Status}.");
    }

    /// <summary>
    /// TCID03 - Abnormal: Rejection reason is missing / empty.
    /// Expects: 400 Bad Request ("A rejection reason is required.").
    /// BR-125
    /// </summary>
    [Fact]
    public async Task TCID03_ReviewAsync_EmptyReason_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var owner = CreateStoreOwnerUser(StoreOwnerId);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(StoreOwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act - reason is empty string
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: false, reason: "   ", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("A rejection reason is required.");

        _output.WriteLine($"[TCID03] Passed: Empty rejection reason correctly threw 400 BadRequest. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID04 - Abnormal: Refund does not exist.
    /// Expects: 404 Not Found ("Refund not found.").
    /// </summary>
    [Fact]
    public async Task TCID04_ReviewAsync_RefundNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _refundRepoMock.Setup(r => r.GetAsync(NonExistentRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        // Act
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, NonExistentRefundId, approve: false, reason: "Rejected", CancellationToken.None);

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
    public async Task TCID05_ReviewAsync_NonOwnerUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(NonOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var act = async () => await _service.ReviewAsync(NonOwnerId, ValidShopId, ValidRefundId, approve: false, reason: "Rejected", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Only the Store Owner may access refund management.");

        _output.WriteLine($"[TCID05] Passed: Non-owner user correctly threw 403 Forbidden. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID06 - Abnormal: Ineligible refund type (ShopCancellation cannot be reviewed).
    /// Expects: 409 Conflict ("Only post-completion customer requests can be reviewed.").
    /// </summary>
    [Fact]
    public async Task TCID06_ReviewAsync_ShopCancellationRefund_ThrowsConflict_Abnormal()
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
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: false, reason: "Rejected", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only post-completion customer requests can be reviewed.");

        _output.WriteLine($"[TCID06] Passed: ShopCancellation refund correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID07 - Abnormal: Refund is not in PendingVerification status (e.g. AwaitingCustomerDetails).
    /// Expects: 409 Conflict ("Review requires PendingVerification and a confirmed bank destination.").
    /// BR-124
    /// </summary>
    [Fact]
    public async Task TCID07_ReviewAsync_StatusNotPendingVerification_ThrowsConflict_Abnormal()
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
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: false, reason: "Rejected", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Review requires PendingVerification and a confirmed bank destination.");

        _output.WriteLine($"[TCID07] Passed: Non-PendingVerification status correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID08 - Abnormal: Shop review deadline expired (ReviewDeadlineAt <= Now).
    /// Expects: 409 Conflict ("The 24-hour shop processing deadline has passed. This refund can no longer be processed.").
    /// BR-196
    /// </summary>
    [Fact]
    public async Task TCID08_ReviewAsync_ReviewDeadlineExpired_ThrowsConflict_Abnormal()
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
        var act = async () => await _service.ReviewAsync(StoreOwnerId, ValidShopId, ValidRefundId, approve: false, reason: "Rejected", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The 24-hour shop processing deadline has passed. This refund can no longer be processed.");

        _output.WriteLine($"[TCID08] Passed: Expired deadline correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }
}
