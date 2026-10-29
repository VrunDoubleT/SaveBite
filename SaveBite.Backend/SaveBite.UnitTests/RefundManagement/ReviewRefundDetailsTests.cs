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
/// Unit Tests for Feature: Review Refund Details (Store Owner)
/// Service: RefundService.GetAsync
/// Use Case: UC-104 (Review Refund Details)
/// Business Rules: BR-120, BR-177
/// Sheet Name: UT_ReviewRefundDetails
/// </summary>
public class ReviewRefundDetailsTests
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
    public static readonly Guid OtherShopId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid ValidCustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidOrderId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid ValidRefundId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid NonExistentRefundId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public ReviewRefundDetailsTests(ITestOutputHelper output)
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
        RefundStatus status = RefundStatus.PendingVerification,
        RefundType type = RefundType.CustomerRequest,
        bool destinationConfirmed = true)
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
            DestinationConfirmedAt = destinationConfirmed ? DateTime.UtcNow.AddMinutes(-30) : null,
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
    /// TCID01 - Normal: Store owner views details of confirmed customer refund in their shop.
    /// Expects: 200 OK, returns RefundResponse.
    /// BR-120, BR-177
    /// </summary>
    [Fact]
    public async Task TCID01_GetAsync_ShopOwner_ValidConfirmedRefund_ReturnsDetails_Normal()
    {
        // Arrange
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, false, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.GetAsync(StoreOwnerId, ValidRefundId, ValidShopId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ValidRefundId);
        result.ShopId.Should().Be(ValidShopId);
        result.Amount.Should().Be(150000m);
        result.Status.Should().Be(nameof(RefundStatus.PendingVerification));

        _output.WriteLine($"[TCID01] Success: Store owner reviewed details of refund {result.Id}. Status: {result.Status}");
    }

    /// <summary>
    /// TCID02 - Abnormal: Refund does not exist.
    /// Expects: 404 Not Found ("Refund not found.").
    /// </summary>
    [Fact]
    public async Task TCID02_GetAsync_RefundNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _refundRepoMock.Setup(r => r.GetAsync(NonExistentRefundId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Refund?)null);

        // Act
        var act = async () => await _service.GetAsync(StoreOwnerId, NonExistentRefundId, ValidShopId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found.");

        _output.WriteLine($"[TCID02] Passed: Non-existent refund correctly threw 404 NotFound. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID03 - Abnormal: Non-owner user attempts to view shop refund details.
    /// Expects: 403 Forbidden ("Only the Store Owner may access refund management.").
    /// BR-177
    /// </summary>
    [Fact]
    public async Task TCID03_GetAsync_NonOwnerUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(ValidRefundId, ValidOrderId, ValidShopId);

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, false, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(NonOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var act = async () => await _service.GetAsync(NonOwnerId, ValidRefundId, ValidShopId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Only the Store Owner may access refund management.");

        _output.WriteLine($"[TCID03] Passed: Non-owner access correctly threw 403 Forbidden. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID04 - Abnormal: Store owner attempts to view refund belonging to a different shop.
    /// Expects: 404 Not Found ("Refund not found in this shop.").
    /// </summary>
    [Fact]
    public async Task TCID04_GetAsync_DifferentShopRefund_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(ValidRefundId, ValidOrderId, OtherShopId); // Belongs to OtherShopId

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, false, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.GetAsync(StoreOwnerId, ValidRefundId, ValidShopId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found in this shop.");

        _output.WriteLine($"[TCID04] Passed: Refund of different shop correctly threw 404 NotFound. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID05 - Abnormal: Store owner attempts to view CustomerRequest refund before destination is confirmed.
    /// Expects: 404 Not Found ("Refund not found in this shop.").
    /// BR-120
    /// </summary>
    [Fact]
    public async Task TCID05_GetAsync_UnconfirmedDestination_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var refund = CreateRefund(
            ValidRefundId,
            ValidOrderId,
            ValidShopId,
            status: RefundStatus.AwaitingCustomerDetails,
            type: RefundType.CustomerRequest,
            destinationConfirmed: false // Unconfirmed
        );

        _refundRepoMock.Setup(r => r.GetAsync(ValidRefundId, false, It.IsAny<CancellationToken>())).ReturnsAsync(refund);
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.GetAsync(StoreOwnerId, ValidRefundId, ValidShopId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Refund not found in this shop.");

        _output.WriteLine($"[TCID05] Passed: Unconfirmed CustomerRequest correctly threw 404 NotFound. Message: {ex.Which.Message}");
    }
}
