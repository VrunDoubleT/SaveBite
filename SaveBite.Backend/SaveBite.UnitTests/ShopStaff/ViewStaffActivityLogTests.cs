using System.Net;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;
using ShopStaffEntity = SaveBite.Backend.Models.Entities.ShopStaff;

namespace SaveBite.Tests.ShopStaffManagement;

/// <summary>
/// Unit Tests for Feature: View Staff Activity Log (Xem lịch sử hoạt động của nhân viên)
/// Service: ShopStaffService.GetStaffActivityLogsAsync
/// Function Code: STAFF-012
/// </summary>
public class ViewStaffActivityLogTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly ShopStaffService _service;

    // Fixed Test Identifiers
    private readonly Guid _ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _staffId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _staffUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly Guid _otherShopId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public ViewStaffActivityLogTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopStaffService(_staffRepoMock.Object);
    }

    // =========================================================================
    // UTCID01: Lấy logs thành công khi nhân viên có lịch sử hoạt động -> Has items (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetStaffActivityLogs_HasItems_ShouldReturnLogs_Normal()
    {
        var shop = new Shop { Id = _shopId, OwnerUserId = _ownerId };
        var staff = new ShopStaffEntity { Id = _staffId, ShopId = _shopId, UserId = _staffUserId };
        var logs = new List<StaffActivityLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                ActionBy = _staffUserId,
                Action = OrderStatus.Completed,
                Note = "Completed order",
                OrderId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            }
        };

        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);
        _staffRepoMock.Setup(r => r.GetStaffByIdAsync(_staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        _staffRepoMock.Setup(r => r.GetStaffActivityLogsPagedAsync(_shopId, _staffUserId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((logs, 1));

        var result = await _service.GetStaffActivityLogsAsync(_ownerId, _staffId, page: 1, pageSize: 10);

        PrintSuccess("UTCID01", $"Retrieved {result.Items.Count()} activity logs successfully.");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().Action.Should().Be(OrderStatus.Completed.ToString());
        result.Items.First().Description.Should().Be("Completed order");
    }

    // =========================================================================
    // UTCID02: Lấy logs thành công khi nhân viên chưa có log nào -> Empty items (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetStaffActivityLogs_EmptyLogs_ShouldReturnEmpty_Normal()
    {
        var shop = new Shop { Id = _shopId, OwnerUserId = _ownerId };
        var staff = new ShopStaffEntity { Id = _staffId, ShopId = _shopId, UserId = _staffUserId };

        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);
        _staffRepoMock.Setup(r => r.GetStaffByIdAsync(_staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        _staffRepoMock.Setup(r => r.GetStaffActivityLogsPagedAsync(_shopId, _staffUserId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<StaffActivityLog>(), 0));

        var result = await _service.GetStaffActivityLogsAsync(_ownerId, _staffId, page: 1, pageSize: 10);

        PrintSuccess("UTCID02", "Retrieved empty activity logs list successfully.");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

   

    // =========================================================================
    // UTCID04: Không tìm thấy nhân viên (staff == null) -> 404 NotFound (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetStaffActivityLogs_StaffNotFound_ThrowsNotFound_Abnormal()
    {
        var shop = new Shop { Id = _shopId, OwnerUserId = _ownerId };

        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);
        _staffRepoMock.Setup(r => r.GetStaffByIdAsync(_staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopStaffEntity?)null);

        var act = () => _service.GetStaffActivityLogsAsync(_ownerId, _staffId, page: 1, pageSize: 10);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.StaffNotFound);
        ex.Which.Message.Should().Be("Staff member not found");
    }

    // =========================================================================
    // UTCID05: Nhân viên thuộc quán khác (staff.ShopId != shop.Id) -> 404 NotFound (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetStaffActivityLogs_DifferentShopStaff_ThrowsNotFound_Abnormal()
    {
        var shop = new Shop { Id = _shopId, OwnerUserId = _ownerId };
        var staff = new ShopStaffEntity { Id = _staffId, ShopId = _otherShopId, UserId = _staffUserId };

        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shop);
        _staffRepoMock.Setup(r => r.GetStaffByIdAsync(_staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var act = () => _service.GetStaffActivityLogsAsync(_ownerId, _staffId, page: 1, pageSize: 10);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.StaffNotFound);
        ex.Which.Message.Should().Be("Staff member not found");
    }

    private void PrintSuccess(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }

    private void PrintException(string tcid, AppException ex)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"ERROR_CODE: {ex.ErrorCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }
}
