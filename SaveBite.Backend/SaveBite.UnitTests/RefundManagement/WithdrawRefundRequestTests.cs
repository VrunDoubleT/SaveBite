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
/// Unit Tests for Feature: Withdraw Refund Request
/// Service: RefundService.WithdrawAsync
/// Use Case: UC-38 (Withdraw Refund Request)
/// Business Rules: BR-045, BR-047, BR-048
/// Sheet Name: UT_WithdrawRefundRequest
/// </summary>
public class WithdrawRefundRequestTests
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
    public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OtherUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid ValidOrderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ValidRefundId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid NonExistentRefundId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid ValidShopId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public WithdrawRefundRequestTests(ITestOutputHelper output)
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

    private User CreateCustomerUser(Guid id, UserStatus status = UserStatus.Active, CustomerStatus custStatus = CustomerStatus.Active)
    {
        return new User
        {
            Id = id,
            FullName = "Nguyen Van A",
            Email = "customer@savebite.com",
            Role = UserRole.User,
            Status = status,
            CustomerStatus = custStatus
        };
    }

    private Refund CreateRefund(
        Guid refundId,
        Guid orderId,
        Guid customerId,
        RefundStatus status = RefundStatus.AwaitingCustomerDetails,
        RefundType type = RefundType.CustomerRequest,
        DateTime? customerDetailsDeadlineAt = null,
        DateTime? reviewDeadlineAt = null)
    {
        var order = new Order
        {
            Id = orderId,
            OrderCode = "ORD-20261029-001",
            CustomerId = customerId,
            ShopId = ValidShopId,
            Status = OrderStatus.Completed,
            TotalAmount = 150000m,
            Shop = new Shop { Id = ValidShopId, Name = "SaveBite Bakery", LogoUrl = "https://savebite.com/logos/shop.png" },
            Customer = new User { Id = customerId, FullName = "Nguyen Van A", Email = "customer@savebite.com", AvatarUrl = "https://savebite.com/avatars/user.png" },
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
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            CustomerDetailsDeadlineAt = customerDetailsDeadlineAt,
            ReviewDeadlineAt = reviewDeadlineAt,
            Order = order,
            History = new List<RefundStatusHistory>(),
            EvidenceImages = new List<RefundEvidenceImage>()
        };

        order.Refunds.Add(refund);
        return refund;
    }

    /// <summary>
    /// TCID01 - Normal: Withdraw refund in AwaitingCustomerDetails status.
    /// Expects: 200 OK, Status transitions to Withdrawn, audit history logged.
    /// BR-047, BR-048
    /// </summary>
    [Fact]
    public async Task TCID01_WithdrawAsync_AwaitingCustomerDetails_WithdrawsSuccessfully_Normal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.AwaitingCustomerDetails,
            type: RefundType.CustomerRequest
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var result = await _service.WithdrawAsync(ValidUserId, ValidRefundId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.Withdrawn));
        refund.Status.Should().Be(RefundStatus.Withdrawn);
        refund.History.Should().ContainSingle(h => h.Decision == "Withdrawn" && h.FromStatus == RefundStatus.AwaitingCustomerDetails);

        _orderRepoMock.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"[TCID01] Success: Refund withdrawn from AwaitingCustomerDetails. New Status: {result.Status}");
    }

    /// <summary>
    /// TCID02 - Normal: Withdraw refund in PendingVerification status.
    /// Expects: 200 OK, Status transitions to Withdrawn, audit history logged.
    /// BR-047, BR-048
    /// </summary>
    [Fact]
    public async Task TCID02_WithdrawAsync_PendingVerification_WithdrawsSuccessfully_Normal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.PendingVerification,
            type: RefundType.CustomerRequest,
            reviewDeadlineAt: DateTime.UtcNow.AddHours(20)
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var result = await _service.WithdrawAsync(ValidUserId, ValidRefundId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.Withdrawn));
        refund.Status.Should().Be(RefundStatus.Withdrawn);
        refund.History.Should().ContainSingle(h => h.Decision == "Withdrawn" && h.FromStatus == RefundStatus.PendingVerification);

        _orderRepoMock.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"[TCID02] Success: Refund withdrawn from PendingVerification. New Status: {result.Status}");
    }

    /// <summary>
    /// TCID03 - Normal: Repeated withdrawal of an already Withdrawn refund.
    /// Expects: 200 OK without re-auditing or state changes (Idempotent).
    /// </summary>
    [Fact]
    public async Task TCID03_WithdrawAsync_AlreadyWithdrawn_ReturnsExistingStatus_Normal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.Withdrawn,
            type: RefundType.CustomerRequest
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var result = await _service.WithdrawAsync(ValidUserId, ValidRefundId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.Withdrawn));
        refund.History.Should().BeEmpty(); // No redundant history entry

        _output.WriteLine($"[TCID03] Success: Idempotent withdrawal returned existing status {result.Status}.");
    }

    /// <summary>
    /// TCID04 - Abnormal: Refund does not exist.
    /// Expects: 404 Not Found ("Refund not found.").
    /// </summary>
    [Fact]
    public async Task TCID04_WithdrawAsync_RefundNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _refundRepoMock.Setup(r => r.GetAsync(NonExistentRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        // Act
        var act = async () => await _service.WithdrawAsync(ValidUserId, NonExistentRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found.");

        _output.WriteLine($"[TCID04] Passed: Non-existent refund correctly threw 404 NotFound. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID05 - Abnormal: Customer account is suspended (CustomerStatus != Active).
    /// Expects: 403 Forbidden ("Active Customer access is required.").
    /// </summary>
    [Fact]
    public async Task TCID05_WithdrawAsync_CustomerSuspended_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId, custStatus: CustomerStatus.Suspended);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidUserId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var act = async () => await _service.WithdrawAsync(ValidUserId, ValidRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Active Customer access is required.");

        _output.WriteLine($"[TCID05] Passed: Suspended user correctly threw 403 Forbidden. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID06 - Abnormal: Refund belongs to another customer.
    /// Expects: 404 Not Found ("Refund not found.").
    /// BR-045
    /// </summary>
    [Fact]
    public async Task TCID06_WithdrawAsync_DifferentCustomer_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, OtherUserId); // Belongs to OtherUserId

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var act = async () => await _service.WithdrawAsync(ValidUserId, ValidRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found.");

        _output.WriteLine($"[TCID06] Passed: Accessing other customer's refund correctly threw 404 NotFound. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID07 - Abnormal: Shop cancellation confirmation deadline passed (> 24 hours).
    /// Expects: 409 Conflict ("The 24-hour confirmation deadline has passed. This refund is no longer available.").
    /// </summary>
    [Fact]
    public async Task TCID07_WithdrawAsync_ShopCancellationDeadlineExpired_ThrowsConflict_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.AwaitingCustomerDetails,
            type: RefundType.ShopCancellation,
            customerDetailsDeadlineAt: DateTime.UtcNow.AddHours(-1)
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var act = async () => await _service.WithdrawAsync(ValidUserId, ValidRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The 24-hour confirmation deadline has passed. This refund is no longer available.");

        _output.WriteLine($"[TCID07] Passed: Expired customer confirmation deadline correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID08 - Abnormal: Shop processing deadline passed (> 24 hours in PendingVerification).
    /// Expects: 409 Conflict ("The 24-hour shop processing deadline has passed. This refund can no longer be processed.").
    /// </summary>
    [Fact]
    public async Task TCID08_WithdrawAsync_ShopProcessingDeadlineExpired_ThrowsConflict_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.PendingVerification,
            type: RefundType.CustomerRequest,
            reviewDeadlineAt: DateTime.UtcNow.AddHours(-1) // Overdue
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var act = async () => await _service.WithdrawAsync(ValidUserId, ValidRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The 24-hour shop processing deadline has passed. This refund can no longer be processed.");

        _output.WriteLine($"[TCID08] Passed: Expired shop processing deadline correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID09 - Abnormal: Refund in ineligible status (e.g. AwaitingTransfer).
    /// Expects: 409 Conflict ("This refund can no longer be withdrawn.").
    /// BR-047
    /// </summary>
    [Fact]
    public async Task TCID09_WithdrawAsync_IneligibleStatus_ThrowsConflict_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.AwaitingTransfer,
            type: RefundType.CustomerRequest
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var act = async () => await _service.WithdrawAsync(ValidUserId, ValidRefundId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("This refund can no longer be withdrawn.");

        _output.WriteLine($"[TCID09] Passed: Ineligible status AwaitingTransfer correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }
}
