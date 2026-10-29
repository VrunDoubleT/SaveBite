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
/// Unit Tests for Feature: View Refund History
/// Service: RefundService.PageAsync
/// Use Cases: UC-34 (Customer View Refund History), UC-103 (Shop View Refund Requests)
/// Business Rules: BR-045, BR-046, BR-120, BR-177
/// Sheet Name: UT_ViewRefundHistory
/// </summary>
public class ViewRefundHistoryTests
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
    public static readonly Guid ValidCustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidOwnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid NonOwnerUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid ValidShopId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid OrderId1 = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid OrderId2 = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid RefundId1 = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid RefundId2 = Guid.Parse("88888888-8888-8888-8888-888888888888");

    public ViewRefundHistoryTests(ITestOutputHelper output)
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
    }

    private User CreateCustomerUser(Guid id, UserStatus status = UserStatus.Active, CustomerStatus custStatus = CustomerStatus.Active, UserRole role = UserRole.User)
    {
        return new User
        {
            Id = id,
            FullName = "Nguyen Van A",
            Email = "customer@savebite.com",
            Role = role,
            Status = status,
            CustomerStatus = custStatus
        };
    }

    private Refund CreateRefund(Guid id, Guid orderId, Guid shopId, Guid customerId, RefundStatus status = RefundStatus.Completed)
    {
        return new Refund
        {
            Id = id,
            OrderId = orderId,
            RefundType = RefundType.CustomerRequest,
            Status = status,
            RequestedAmount = 150000m,
            TransferredAmount = status == RefundStatus.Completed ? 150000m : 0m,
            Reason = "Food quality issue",
            DestinationConfirmedAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-30),
            Order = new Order
            {
                Id = orderId,
                OrderCode = "ORD-20261028-001",
                ShopId = shopId,
                CustomerId = customerId,
                Status = OrderStatus.Completed,
                TotalAmount = 150000m,
                IsPlatformFeeExempt = false,
                Shop = new Shop
                {
                    Id = shopId,
                    Name = "SaveBite Bakery",
                    LogoUrl = "https://savebite.com/logos/shop1.png"
                },
                Customer = new User
                {
                    Id = customerId,
                    FullName = "Nguyen Van A",
                    AvatarUrl = "https://savebite.com/avatars/user1.png",
                    Phone = "0901234567"
                }
            },
            EvidenceImages = new List<RefundEvidenceImage>(),
            History = new List<RefundStatusHistory>()
        };
    }

    // =========================================================================
    // TCID01: Customer Scope - Valid Request & No Filters -> Returns Refunds List (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_PageAsync_CustomerScope_ValidRequest_ReturnsRefundsList_Normal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidCustomerId);
        var refund1 = CreateRefund(RefundId1, OrderId1, ValidShopId, ValidCustomerId, RefundStatus.Completed);
        var refund2 = CreateRefund(RefundId2, OrderId2, ValidShopId, ValidCustomerId, RefundStatus.AwaitingTransfer);
        var refundList = new List<Refund> { refund1, refund2 };

        _refundRepoMock
            .Setup(r => r.UserAsync(ValidCustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _refundRepoMock
            .Setup(r => r.PageAsync(ValidCustomerId, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((refundList, 2));

        // Act
        var result = await _service.PageAsync(ValidCustomerId, null, null, null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalItems.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items[0].Id.Should().Be(RefundId1);
        result.Items[0].CustomerName.Should().Be("Nguyen Van A");
        result.Items[0].ShopName.Should().Be("SaveBite Bakery");
        result.Items[1].Id.Should().Be(RefundId2);

        _output.WriteLine($"[PASS] TCID01: Customer retrieved {result.Items.Count} refund records successfully.");
    }

    // =========================================================================
    // TCID02: Store Owner Scope - With Valid Status Filter -> Returns Filtered List (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_PageAsync_ShopOwnerScope_WithValidStatusFilter_ReturnsFilteredList_Normal()
    {
        // Arrange
        var refund = CreateRefund(RefundId1, OrderId1, ValidShopId, ValidCustomerId, RefundStatus.Completed);
        var filteredList = new List<Refund> { refund };

        _shopRepoMock
            .Setup(s => s.RequireAccessAsync(ValidOwnerId, ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _refundRepoMock
            .Setup(r => r.PageAsync(ValidOwnerId, ValidShopId, RefundStatus.Completed, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((filteredList, 1));

        // Act
        var result = await _service.PageAsync(ValidOwnerId, ValidShopId, "Completed", null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalItems.Should().Be(1);
        result.Items[0].Status.Should().Be("Completed");
        result.Items[0].ShopId.Should().Be(ValidShopId);

        _output.WriteLine($"[PASS] TCID02: Store Owner retrieved {result.Items.Count} filtered refund records successfully.");
    }

    // =========================================================================
    // TCID03: Valid Query - No Refund Records Found -> Returns Empty Result (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID03_PageAsync_ValidRequest_NoRecordsFound_ReturnsEmptyPagedResult_Normal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidCustomerId);

        _refundRepoMock
            .Setup(r => r.UserAsync(ValidCustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _refundRepoMock
            .Setup(r => r.PageAsync(ValidCustomerId, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Refund>(), 0));

        // Act
        var result = await _service.PageAsync(ValidCustomerId, null, null, null, 1, 10, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);

        _output.WriteLine("[PASS] TCID03: Query returned empty list as expected when no records exist.");
    }

    // =========================================================================
    // TCID04: Customer Scope - Customer Not Active -> Throws 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_PageAsync_CustomerScope_InactiveCustomer_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var inactiveCustomer = CreateCustomerUser(ValidCustomerId, custStatus: CustomerStatus.Suspended);

        _refundRepoMock
            .Setup(r => r.UserAsync(ValidCustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveCustomer);

        // Act
        var act = async () => await _service.PageAsync(ValidCustomerId, null, null, null, 1, 10, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Active Customer access is required.");

        _output.WriteLine("[PASS] TCID04: Correctly rejected suspended customer with 403 Forbidden.");
    }

    // =========================================================================
    // TCID05: Store Owner Scope - Non-Owner User -> Throws 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID05_PageAsync_ShopOwnerScope_NonOwnerUser_ThrowsForbidden_Abnormal()
    {
        // Arrange
        _shopRepoMock
            .Setup(s => s.RequireAccessAsync(NonOwnerUserId, ValidShopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _service.PageAsync(NonOwnerUserId, ValidShopId, null, null, 1, 10, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Only the Store Owner may access refund management.");

        _output.WriteLine("[PASS] TCID05: Correctly rejected unauthorized shop access with 403 Forbidden.");
    }

    // =========================================================================
    // TCID06: Invalid Status Filter -> Throws 400 Bad Request (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID06_PageAsync_InvalidStatusFilter_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var customer = CreateCustomerUser(ValidCustomerId);

        _refundRepoMock
            .Setup(r => r.UserAsync(ValidCustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var act = async () => await _service.PageAsync(ValidCustomerId, null, "NonExistentStatus", null, 1, 10, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<AppException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("Invalid refund filter.");

        _output.WriteLine("[PASS] TCID06: Correctly rejected invalid status filter with 400 Bad Request.");
    }
}
