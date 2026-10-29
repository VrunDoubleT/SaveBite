using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
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
/// Unit Tests for Feature: View Refund Requests (Store Owner)
/// Service: RefundService.PageAsync
/// Use Case: UC-103 (View Refund Requests)
/// Business Rules: BR-120, BR-177, BR-196
/// Sheet Name: UT_ViewRefundRequests
/// </summary>
public class ViewRefundRequestsTests
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

    public ViewRefundRequestsTests(ITestOutputHelper output)
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
        RefundStatus status = RefundStatus.PendingVerification,
        RefundType type = RefundType.CustomerRequest)
    {
        var order = new Order
        {
            Id = orderId,
            OrderCode = "ORD-20261029-001",
            CustomerId = ValidCustomerId,
            ShopId = ValidShopId,
            Status = OrderStatus.Completed,
            TotalAmount = 150000m,
            Shop = new Shop { Id = ValidShopId, Name = "SaveBite Bakery", LogoUrl = "https://savebite.com/logos/shop.png" },
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
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            DestinationConfirmedAt = DateTime.UtcNow.AddMinutes(-30),
            ReviewDeadlineAt = DateTime.UtcNow.AddHours(11), // within 12 hours BR-196
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
    /// TCID01 - Normal: Store owner views refund requests for their shop without filter.
    /// Expects: 200 OK, returns PagedResult with records matching shop.
    /// BR-120, BR-177
    /// </summary>
    [Fact]
    public async Task TCID01_PageAsync_ShopOwnerScope_ReturnsRefundRequests_Normal()
    {
        // Arrange
        var refund1 = CreateRefund(Guid.NewGuid(), Guid.NewGuid(), RefundStatus.PendingVerification);
        var refund2 = CreateRefund(Guid.NewGuid(), Guid.NewGuid(), RefundStatus.AwaitingTransfer);

        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _refundRepoMock.Setup(r => r.PageAsync(
            StoreOwnerId,
            ValidShopId,
            null,
            null,
            1,
            10,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync((new List<Refund> { refund1, refund2 }, 2));

        // Act
        var result = await _service.PageAsync(StoreOwnerId, ValidShopId, null, null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);

        _output.WriteLine($"[TCID01] Success: Store owner retrieved {result.Items.Count} refund requests.");
    }

    /// <summary>
    /// TCID02 - Normal: Store owner filters refund requests by "PendingVerification".
    /// Expects: 200 OK, returns filtered PagedResult.
    /// BR-196
    /// </summary>
    [Fact]
    public async Task TCID02_PageAsync_ShopOwnerScope_PendingVerificationFilter_ReturnsFilteredList_Normal()
    {
        // Arrange
        var refund = CreateRefund(Guid.NewGuid(), Guid.NewGuid(), RefundStatus.PendingVerification);

        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _refundRepoMock.Setup(r => r.PageAsync(
            StoreOwnerId,
            ValidShopId,
            RefundStatus.PendingVerification,
            null,
            1,
            10,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync((new List<Refund> { refund }, 1));

        // Act
        var result = await _service.PageAsync(StoreOwnerId, ValidShopId, "PendingVerification", null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalItems.Should().Be(1);
        result.Items[0].Status.Should().Be(nameof(RefundStatus.PendingVerification));

        _output.WriteLine($"[TCID02] Success: Filtered list returned 1 PendingVerification item.");
    }

    /// <summary>
    /// TCID03 - Normal: Store owner views refund requests when no matching records exist.
    /// Expects: 200 OK, returns empty PagedResult (0 items).
    /// </summary>
    [Fact]
    public async Task TCID03_PageAsync_ShopOwnerScope_NoRecordsFound_ReturnsEmptyPagedResult_Normal()
    {
        // Arrange
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _refundRepoMock.Setup(r => r.PageAsync(
            StoreOwnerId,
            ValidShopId,
            null,
            null,
            1,
            10,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync((new List<Refund>(), 0));

        // Act
        var result = await _service.PageAsync(StoreOwnerId, ValidShopId, null, null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);

        _output.WriteLine($"[TCID03] Success: Empty list returned as expected.");
    }

    /// <summary>
    /// TCID04 - Abnormal: Non-owner user attempts to view shop refund requests.
    /// Expects: 403 Forbidden ("Only the Store Owner may access refund management.").
    /// BR-177
    /// </summary>
    [Fact]
    public async Task TCID04_PageAsync_ShopOwnerScope_NonOwner_ThrowsForbidden_Abnormal()
    {
        // Arrange
        _shopRepoMock.Setup(s => s.RequireAccessAsync(NonOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var act = async () => await _service.PageAsync(NonOwnerId, ValidShopId, null, null, 1, 10, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Only the Store Owner may access refund management.");

        _output.WriteLine($"[TCID04] Passed: Non-owner user correctly threw 403 Forbidden. Message: {ex.Which.Message}");
    }

    /// <summary>
    /// TCID05 - Abnormal: Store owner supplies an invalid status filter.
    /// Expects: 400 Bad Request ("Invalid refund filter.").
    /// </summary>
    [Fact]
    public async Task TCID05_PageAsync_ShopOwnerScope_InvalidFilter_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        _shopRepoMock.Setup(s => s.RequireAccessAsync(StoreOwnerId, ValidShopId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = async () => await _service.PageAsync(StoreOwnerId, ValidShopId, "NonExistentStatus", null, 1, 10, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Invalid refund filter.");

        _output.WriteLine($"[TCID05] Passed: Invalid status filter correctly threw 400 BadRequest. Message: {ex.Which.Message}");
    }
}
