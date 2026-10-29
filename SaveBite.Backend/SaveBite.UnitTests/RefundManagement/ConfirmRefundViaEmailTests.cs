using System.Net;
using System.Security.Cryptography;
using System.Text;
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
/// Unit Tests for Feature: Confirm Refund via Email
/// Service: RefundService.VerifyAsync
/// Use Case: UC-37 (Confirm Refund via Email)
/// Business Rules: BR-178, BR-179, BR-180, BR-181
/// Sheet Name: UT_ConfirmRefundViaEmail
/// </summary>
public class ConfirmRefundViaEmailTests
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

    public const string SecretKey = "SaveBite_Super_Secret_Unit_Testing_Key_2026_Secure!";

    // Explicit GUIDs for test determinism and traceability
    public static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidOrderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ValidRefundId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid NonExistentRefundId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid ValidShopId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public ConfirmRefundViaEmailTests(ITestOutputHelper output)
    {
        _output = output;
        _configMock.Setup(c => c["Jwt:SigningKey"]).Returns(SecretKey);

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

    private static string GenerateCodeHash(Guid refundId, string code)
    {
        return Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(SecretKey),
            Encoding.UTF8.GetBytes($"{refundId:N}:{code}")
        ));
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
        DateTime? verificationExpiresAt = null,
        int verificationAttempts = 0,
        string? verificationCode = "123456",
        DateTime? destinationConfirmedAt = null)
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
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            CustomerDetailsDeadlineAt = customerDetailsDeadlineAt,
            ReceiverBankBin = "970422",
            ReceiverBankName = "MBBank",
            ReceiverAccountNumber = "0123456789",
            ReceiverAccountHolder = "NGUYEN VAN A",
            VerificationCodeHash = verificationCode != null ? GenerateCodeHash(refundId, verificationCode) : null,
            VerificationExpiresAt = verificationExpiresAt ?? DateTime.UtcNow.AddMinutes(15),
            VerificationAttempts = verificationAttempts,
            DestinationConfirmedAt = destinationConfirmedAt,
            Order = order,
            History = new List<RefundStatusHistory>(),
            EvidenceImages = new List<RefundEvidenceImage>()
        };

        order.Refunds.Add(refund);
        return refund;
    }

    /// <summary>
    /// TCID01 - Normal: Confirm refund destination with valid code within expiry window.
    /// Expects: 200 OK, Status transitions to PendingVerification, DestinationConfirmedAt set, history logged.
    /// BR-178, BR-179
    /// </summary>
    [Fact]
    public async Task TCID01_VerifyAsync_ValidCode_ConfirmsDestination_Normal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.AwaitingCustomerDetails,
            type: RefundType.CustomerRequest,
            verificationExpiresAt: DateTime.UtcNow.AddMinutes(10),
            verificationAttempts: 0,
            verificationCode: "123456"
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var result = await _service.VerifyAsync(ValidUserId, ValidRefundId, "123456", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.Status.Should().Be(nameof(RefundStatus.PendingVerification));
        refund.DestinationConfirmedAt.Should().NotBeNull();
        refund.PendingVerificationAt.Should().NotBeNull();
        refund.Status.Should().Be(RefundStatus.PendingVerification);
        refund.VerificationAttempts.Should().Be(1);
        refund.History.Should().ContainSingle(h => h.Decision == "DestinationConfirmed");

        _orderRepoMock.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine($"[TCID01] Success: Destination confirmed. New Status={result.Status}, DestinationConfirmedAt={refund.DestinationConfirmedAt}");
    }

    /// <summary>
    /// TCID02 - Normal: Repeated confirmation with same valid code.
    /// Expects: 200 OK without re-triggering transitions or errors (Idempotency).
    /// BR-181
    /// </summary>
    [Fact]
    public async Task TCID02_VerifyAsync_RepeatedValidCode_ReturnsExistingConfirmation_Normal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var confirmedAt = DateTime.UtcNow.AddMinutes(-5);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.PendingVerification,
            type: RefundType.CustomerRequest,
            verificationExpiresAt: DateTime.UtcNow.AddMinutes(10),
            verificationAttempts: 1,
            verificationCode: "123456",
            destinationConfirmedAt: confirmedAt
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var result = await _service.VerifyAsync(ValidUserId, ValidRefundId, "123456", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        refund.DestinationConfirmedAt.Should().Be(confirmedAt);
        // History should not have a duplicate DestinationConfirmed added
        refund.History.Should().BeEmpty();

        _output.WriteLine($"[TCID02] Success: Idempotent confirmation returned existing status {result.Status} without duplicate changes.");
    }

    /// <summary>
    /// TCID03 - Abnormal: Refund does not exist.
    /// Expects: 404 Not Found ("Refund not found.").
    /// </summary>
    [Fact]
    public async Task TCID03_VerifyAsync_RefundNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _refundRepoMock.Setup(r => r.GetAsync(NonExistentRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        // Act
        var act = async () => await _service.VerifyAsync(ValidUserId, NonExistentRefundId, "123456", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found.");

        _output.WriteLine($"[TCID03] Passed: Non-existent refund correctly threw 404 NotFound. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID04 - Abnormal: User account is suspended (CustomerStatus != Active).
    /// Expects: 403 Forbidden ("Active Customer access is required.").
    /// </summary>
    [Fact]
    public async Task TCID04_VerifyAsync_CustomerSuspended_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId, custStatus: CustomerStatus.Suspended);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidUserId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var act = async () => await _service.VerifyAsync(ValidUserId, ValidRefundId, "123456", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Active Customer access is required.");

        _output.WriteLine($"[TCID04] Passed: Suspended user correctly threw 403 Forbidden. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID05 - Abnormal: Refund has already been closed/rejected (Status == Rejected).
    /// Expects: 409 Conflict ("This refund has been closed.").
    /// </summary>
    [Fact]
    public async Task TCID05_VerifyAsync_RefundRejected_ThrowsConflict_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidUserId, status: RefundStatus.Rejected);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var act = async () => await _service.VerifyAsync(ValidUserId, ValidRefundId, "123456", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("This refund has been closed.");

        _output.WriteLine($"[TCID05] Passed: Closed refund correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID06 - Abnormal: Shop cancellation confirmation deadline (> 24 hours) expired.
    /// Expects: 409 Conflict ("The 24-hour confirmation deadline has passed. This refund is no longer available.").
    /// </summary>
    [Fact]
    public async Task TCID06_VerifyAsync_ShopCancellationDeadlineExpired_ThrowsConflict_Abnormal()
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
        var act = async () => await _service.VerifyAsync(ValidUserId, ValidRefundId, "123456", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The 24-hour confirmation deadline has passed. This refund is no longer available.");

        _output.WriteLine($"[TCID06] Passed: Expired deadline correctly threw 409 Conflict. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID07 - Abnormal: Confirmation code has expired (VerificationExpiresAt <= Now).
    /// Expects: 400 Bad Request ("The confirmation code is invalid, expired, or has reached its attempt limit.").
    /// BR-180
    /// </summary>
    [Fact]
    public async Task TCID07_VerifyAsync_CodeExpired_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.AwaitingCustomerDetails,
            verificationExpiresAt: DateTime.UtcNow.AddMinutes(-1), // Expired
            verificationAttempts: 0,
            verificationCode: "123456"
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var act = async () => await _service.VerifyAsync(ValidUserId, ValidRefundId, "123456", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("The confirmation code is invalid, expired, or has reached its attempt limit.");

        _output.WriteLine($"[TCID07] Passed: Expired code correctly threw 400 BadRequest. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID08 - Abnormal: Exceeded maximum verification attempts (VerificationAttempts >= 5).
    /// Expects: 400 Bad Request ("The confirmation code is invalid, expired, or has reached its attempt limit.").
    /// </summary>
    [Fact]
    public async Task TCID08_VerifyAsync_AttemptsExceeded_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.AwaitingCustomerDetails,
            verificationExpiresAt: DateTime.UtcNow.AddMinutes(10),
            verificationAttempts: 5, // Max limit reached
            verificationCode: "123456"
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act
        var act = async () => await _service.VerifyAsync(ValidUserId, ValidRefundId, "123456", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("The confirmation code is invalid, expired, or has reached its attempt limit.");

        _output.WriteLine($"[TCID08] Passed: Attempts exceeded correctly threw 400 BadRequest. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID09 - Abnormal: Incorrect verification code entered.
    /// Expects: Increments VerificationAttempts and throws 400 Bad Request ("The confirmation code is invalid, expired, or has reached its attempt limit.").
    /// </summary>
    [Fact]
    public async Task TCID09_VerifyAsync_IncorrectCode_IncrementsAttempts_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            status: RefundStatus.AwaitingCustomerDetails,
            verificationExpiresAt: DateTime.UtcNow.AddMinutes(10),
            verificationAttempts: 0,
            verificationCode: "123456" // correct code is 123456
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _refundRepoMock.Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>())).ReturnsAsync(refund.Order);
        _refundRepoMock.Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        // Act - enter wrong code 999999
        var act = async () => await _service.VerifyAsync(ValidUserId, ValidRefundId, "999999", CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("The confirmation code is invalid, expired, or has reached its attempt limit.");
        refund.VerificationAttempts.Should().Be(1);

        _output.WriteLine($"[TCID09] Passed: Incorrect code incremented attempts to {refund.VerificationAttempts} and threw 400 BadRequest.");
    }
}
