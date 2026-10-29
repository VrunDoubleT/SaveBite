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
/// Unit Tests for Feature: Request Refund
/// Service: RefundService.RequestAsync
/// Use Case: UC-35 (Request Refund)
/// Business Rules: BR-035, BR-036, BR-037, BR-038
/// Sheet Name: UT_RequestRefund
/// </summary>
public class RequestRefundTests
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
    public static readonly Guid NonExistentOrderId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid ValidShopId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid ExistingRefundId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public RequestRefundTests(ITestOutputHelper output)
    {
        _output = output;
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

    private Order CreateOrder(
        Guid orderId,
        Guid customerId,
        OrderStatus status = OrderStatus.Completed,
        DateTime? completedAt = null,
        decimal amount = 150000m,
        List<Refund>? refunds = null)
    {
        return new Order
        {
            Id = orderId,
            OrderCode = "ORD-20261029-001",
            CustomerId = customerId,
            ShopId = ValidShopId,
            Status = status,
            CompletedAt = completedAt ?? DateTime.UtcNow.AddHours(-2),
            TotalAmount = amount,
            Shop = new Shop
            {
                Id = ValidShopId,
                Name = "SaveBite Bakery",
                LogoUrl = "https://savebite.com/logos/shop.png"
            },
            Customer = new User
            {
                Id = customerId,
                FullName = "Nguyen Van A",
                AvatarUrl = "https://savebite.com/avatars/user.png",
                Phone = "0901234567"
            },
            Payments = amount > 0 ? new List<Payment>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Amount = amount,
                    Status = PaymentStatus.Succeeded,
                    PaymentType = PaymentType.Full,
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                }
            } : new List<Payment>(),
            Refunds = refunds ?? new List<Refund>()
        };
    }

    private CreateRefundRequest CreateRequest(
        string reason = "Food was spoiled upon opening",
        List<string>? evidenceUrls = null)
    {
        return new CreateRefundRequest
        {
            Reason = reason,
            EvidenceUrls = evidenceUrls ?? new List<string> { "https://savebite.com/evidence1.png" }
        };
    }

    // =========================================================================
    // TCID01: Valid Request -> Creates Refund with Full Amount (Normal - N)
    // BR-035, BR-036, BR-037, BR-038
    // =========================================================================
    [Fact]
    public async Task TCID01_RequestAsync_ValidRequest_CreatesRefund_Normal()
    {
        // Arrange
        var order = CreateOrder(ValidOrderId, ValidUserId);
        var request = CreateRequest();

        _orderRepoMock
            .Setup(o => o.GetOrderAsync(ValidOrderId, ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _service.RequestAsync(ValidUserId, ValidOrderId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(ValidOrderId);
        result.Type.Should().Be("CustomerRequest");
        result.Status.Should().Be("AwaitingCustomerDetails");
        result.Amount.Should().Be(150000m);
        result.Reason.Should().Be("Food was spoiled upon opening");
        result.EvidenceUrls.Should().ContainSingle().Which.Should().Be("https://savebite.com/evidence1.png");

        _refundRepoMock.Verify(r => r.Add(It.IsAny<Refund>()), Times.Once);
        _orderRepoMock.Verify(o => o.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);

        _output.WriteLine($"[PASS] TCID01: Successfully created refund request {result.Id} with status AwaitingCustomerDetails (BR-035, BR-036, BR-037, BR-038).");
    }

    // =========================================================================
    // TCID02: Order Not Found -> Throws 404 NotFound (Abnormal - A)
    // BR-035: Only the Customer of the order can request refund
    // =========================================================================
    [Fact]
    public async Task TCID02_RequestAsync_OrderNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = CreateRequest();

        _orderRepoMock
            .Setup(o => o.GetOrderAsync(NonExistentOrderId, ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var act = async () => await _service.RequestAsync(ValidUserId, NonExistentOrderId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Order not found.");

        _output.WriteLine("[PASS] TCID02: Correctly threw 404 NotFound for non-existent order (BR-035).");
    }

    // =========================================================================
    // TCID03: Order Not Completed -> Throws 409 Conflict (Abnormal - A)
    // BR-035: Cancelled, Rejected, and No-show orders are ineligible
    // =========================================================================
    [Fact]
    public async Task TCID03_RequestAsync_OrderNotCompleted_ThrowsConflict_Abnormal()
    {
        // Arrange
        var order = CreateOrder(ValidOrderId, ValidUserId, status: OrderStatus.Confirmed);
        var request = CreateRequest();

        _orderRepoMock
            .Setup(o => o.GetOrderAsync(ValidOrderId, ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = async () => await _service.RequestAsync(ValidUserId, ValidOrderId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Only a Completed order is eligible for a customer refund request.");

        _output.WriteLine("[PASS] TCID03: Correctly rejected non-completed order with 409 Conflict (BR-035).");
    }

    // =========================================================================
    // TCID04: Submitted > 6 Hours After Completion -> Throws 409 Conflict (Abnormal - A)
    // BR-036: Must submit within 6 hours of completion
    // =========================================================================
    [Fact]
    public async Task TCID04_RequestAsync_SubmittedAfterSixHours_ThrowsConflict_Abnormal()
    {
        // Arrange
        var order = CreateOrder(ValidOrderId, ValidUserId, completedAt: DateTime.UtcNow.AddHours(-7));
        var request = CreateRequest();

        _orderRepoMock
            .Setup(o => o.GetOrderAsync(ValidOrderId, ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = async () => await _service.RequestAsync(ValidUserId, ValidOrderId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Refund requests must be submitted within six hours of order completion.");

        _output.WriteLine("[PASS] TCID04: Correctly rejected expired refund window (> 6 hours) with 409 Conflict (BR-036).");
    }

    // =========================================================================
    // TCID05: Empty Refund Reason -> Throws 400 Bad Request (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_RequestAsync_EmptyReason_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var order = CreateOrder(ValidOrderId, ValidUserId);
        var request = CreateRequest(reason: "");

        _orderRepoMock
            .Setup(o => o.GetOrderAsync(ValidOrderId, ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = async () => await _service.RequestAsync(ValidUserId, ValidOrderId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("A refund reason is required.");

        _output.WriteLine("[PASS] TCID05: Correctly rejected empty reason with 400 Bad Request.");
    }

    // =========================================================================
    // TCID06: Empty Evidence Images -> Throws 400 Bad Request (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID06_RequestAsync_EmptyEvidenceUrls_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var order = CreateOrder(ValidOrderId, ValidUserId);
        var request = CreateRequest(evidenceUrls: new List<string>());

        _orderRepoMock
            .Setup(o => o.GetOrderAsync(ValidOrderId, ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = async () => await _service.RequestAsync(ValidUserId, ValidOrderId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Supply between one and ten evidence images.");

        _output.WriteLine("[PASS] TCID06: Correctly rejected empty evidence with 400 Bad Request.");
    }

    // =========================================================================
    // TCID07: Active Refund Already Exists -> Throws 409 Conflict (Abnormal - A)
    // BR-038: At most one refund request may remain active
    // =========================================================================
    [Fact]
    public async Task TCID07_RequestAsync_ActiveRefundAlreadyExists_ThrowsConflict_Abnormal()
    {
        // Arrange
        var existingActiveRefund = new Refund
        {
            Id = ExistingRefundId,
            OrderId = ValidOrderId,
            Status = RefundStatus.PendingVerification
        };
        var order = CreateOrder(ValidOrderId, ValidUserId, refunds: new List<Refund> { existingActiveRefund });
        var request = CreateRequest();

        _orderRepoMock
            .Setup(o => o.GetOrderAsync(ValidOrderId, ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = async () => await _service.RequestAsync(ValidUserId, ValidOrderId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("An active refund already exists for this order.");

        _output.WriteLine("[PASS] TCID07: Correctly rejected duplicate active refund request with 409 Conflict (BR-038).");
    }

    // =========================================================================
    // TCID08: No Refundable Balance Remaining -> Throws 409 Conflict (Abnormal - A)
    // BR-037: Full eligible amount actually paid minus amounts already refunded
    // =========================================================================
    [Fact]
    public async Task TCID08_RequestAsync_NoRefundableBalanceRemaining_ThrowsConflict_Abnormal()
    {
        // Arrange: Order was paid 150,000 but already fully refunded in a prior closed refund
        var previousRefund = new Refund
        {
            Id = ExistingRefundId,
            OrderId = ValidOrderId,
            Status = RefundStatus.Completed,
            RequestedAmount = 150000m,
            TransferredAmount = 150000m
        };
        var order = CreateOrder(ValidOrderId, ValidUserId, refunds: new List<Refund> { previousRefund });
        var request = CreateRequest();

        _orderRepoMock
            .Setup(o => o.GetOrderAsync(ValidOrderId, ValidUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _refundRepoMock
            .Setup(r => r.OrderAsync(ValidOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = async () => await _service.RequestAsync(ValidUserId, ValidOrderId, request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("This order has no refundable paid balance.");

        _output.WriteLine("[PASS] TCID08: Correctly rejected refund when order has no remaining balance with 409 Conflict (BR-037).");
    }
}
