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
/// Unit Tests for Feature: Provide Bank Information
/// Service: RefundService.DestinationAsync
/// Use Case: UC-36 (Provide Bank Information)
/// Business Rules: BR-178, BR-179, BR-180, BR-181
/// Sheet Name: UT_ProvideBankInformation
/// </summary>
public class ProvideBankInformationTests
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
    public static readonly Guid ValidOrderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ValidRefundId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid NonExistentRefundId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid ValidShopId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public ProvideBankInformationTests(ITestOutputHelper output)
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
        DateTime? verificationIssuedAt = null)
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
            Reason = "Food quality issue",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            CustomerDetailsDeadlineAt = customerDetailsDeadlineAt,
            VerificationIssuedAt = verificationIssuedAt,
            Order = order,
            EvidenceImages = new List<RefundEvidenceImage>(),
            History = new List<RefundStatusHistory>()
        };

        order.Refunds.Add(refund);
        return refund;
    }

    private RefundBankRequest CreateBankRequest(
        string bankBin = "970422",
        string bankName = "MBBank",
        string accountNumber = "0123456789",
        string accountHolder = "NGUYEN VAN A",
        string receiverMethod = "BankAccount")
    {
        return new RefundBankRequest
        {
            BankBin = bankBin,
            BankName = bankName,
            AccountNumber = accountNumber,
            AccountHolder = accountHolder,
            ReceiverMethod = receiverMethod
        };
    }

    // =========================================================================
    // TCID01: Valid Bank Information -> Stores Destination & Sends Verification Email (Normal - N)
    // BR-178, BR-179, BR-180
    // =========================================================================
    [Fact]
    public async Task TCID01_DestinationAsync_ValidBankInfo_SendsEmailVerification_Normal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidUserId);
        var request = CreateBankRequest();

        _refundRepoMock
            .Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund.Order);

        _refundRepoMock
            .Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _service.DestinationAsync(ValidUserId, ValidRefundId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.BankBin.Should().Be("970422");
        result.BankName.Should().Be("MBBank");
        result.AccountNumber.Should().Be("0123456789");
        result.AccountHolder.Should().Be("NGUYEN VAN A");
        result.ConfirmationExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(14));

        _emailMock.Verify(e => e.SendAsync(
            "customer@savebite.com",
            "Confirm your SaveBite refund destination",
            It.Is<string>(body => body.Contains("Your refund confirmation code is") && body.Contains("MBBank")),
            false,
            It.IsAny<CancellationToken>()), Times.Once);

        _orderRepoMock.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[PASS] TCID01: Stored bank destination and issued 15-minute verification email (BR-178, BR-179, BR-180).");
    }

    // =========================================================================
    // TCID02: Invalid Bank BIN -> Throws 400 Bad Request (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_DestinationAsync_InvalidBankBin_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = CreateBankRequest(bankBin: "123"); // Invalid: not 6 digits

        // Act
        var act = async () => await _service.DestinationAsync(ValidUserId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Supply a valid bank BIN, bank name, account number and account holder.");

        _output.WriteLine("[PASS] TCID02: Correctly rejected invalid bank BIN with 400 Bad Request.");
    }

    // =========================================================================
    // TCID03: Refund Not Found -> Throws 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_DestinationAsync_RefundNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = CreateBankRequest();

        _refundRepoMock
            .Setup(r => r.GetAsync(NonExistentRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        // Act
        var act = async () => await _service.DestinationAsync(ValidUserId, NonExistentRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found.");

        _output.WriteLine("[PASS] TCID03: Correctly threw 404 NotFound for non-existent refund.");
    }

    // =========================================================================
    // TCID04: Customer Suspended -> Throws 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_DestinationAsync_CustomerSuspended_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId, custStatus: CustomerStatus.Suspended);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidUserId);
        var request = CreateBankRequest();

        _refundRepoMock
            .Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund.Order);

        _refundRepoMock
            .Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var act = async () => await _service.DestinationAsync(ValidUserId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Active Customer access is required.");

        _output.WriteLine("[PASS] TCID04: Correctly rejected suspended customer with 403 Forbidden.");
    }

    // =========================================================================
    // TCID05: Refund Status Not AwaitingCustomerDetails -> Throws 409 Conflict (Abnormal - A)
    // BR-178: Destination can no longer be changed once past customer details phase
    // =========================================================================
    [Fact]
    public async Task TCID05_DestinationAsync_StatusNotAwaitingCustomerDetails_ThrowsConflict_Abnormal()
    {
        // Arrange: Refund already progressed to PendingVerification
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidUserId, status: RefundStatus.PendingVerification);
        var request = CreateBankRequest();

        _refundRepoMock
            .Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund.Order);

        _refundRepoMock
            .Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var act = async () => await _service.DestinationAsync(ValidUserId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The destination can no longer be changed.");

        _output.WriteLine("[PASS] TCID05: Correctly rejected changing destination for non-awaiting status with 409 Conflict (BR-178).");
    }

    // =========================================================================
    // TCID06: Shop Cancellation Deadline Expired (> 24h) -> Throws 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID06_DestinationAsync_ShopCancellationDeadlineExpired_ThrowsConflict_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            type: RefundType.ShopCancellation,
            customerDetailsDeadlineAt: DateTime.UtcNow.AddMinutes(-5)); // Expired
        var request = CreateBankRequest();

        _refundRepoMock
            .Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund.Order);

        _refundRepoMock
            .Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var act = async () => await _service.DestinationAsync(ValidUserId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The 24-hour confirmation deadline has passed. This refund is no longer available.");

        _output.WriteLine("[PASS] TCID06: Correctly rejected expired 24h deadline with 409 Conflict.");
    }

    // =========================================================================
    // TCID07: Requested Interval < 1 Minute -> Throws 409 Conflict (Abnormal - A)
    // BR-181: Rate limit code requests
    // =========================================================================
    [Fact]
    public async Task TCID07_DestinationAsync_RequestedWithinOneMinute_ThrowsConflict_Abnormal()
    {
        // Arrange: Verification was issued 30 seconds ago
        var customer = CreateCustomerUser(ValidUserId);
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidUserId,
            verificationIssuedAt: DateTime.UtcNow.AddSeconds(-30));
        var request = CreateBankRequest();

        _refundRepoMock
            .Setup(r => r.GetAsync(ValidRefundId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund.Order);

        _refundRepoMock
            .Setup(r => r.UserAsync(ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var act = async () => await _service.DestinationAsync(ValidUserId, ValidRefundId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Wait one minute before requesting another code.");

        _output.WriteLine("[PASS] TCID07: Correctly throttled repeated request within one minute with 409 Conflict (BR-181).");
    }
}
