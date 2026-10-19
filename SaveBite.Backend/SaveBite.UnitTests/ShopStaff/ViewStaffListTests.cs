using System.Net;
using FluentAssertions;
using Moq;
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
/// Unit Tests for Feature: Get Shop Staffs (View Staff List)
/// Service: ShopStaffService.GetShopStaffsAsync
/// Function Code: STAFF-008
/// </summary>
public class ViewStaffListTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly ShopStaffService _service;

    // Fixed Test Identifiers
    private readonly Guid _ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Shop _ownerShop;

    public ViewStaffListTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopStaffService(_staffRepoMock.Object);

        _ownerShop = new Shop
        {
            Id = _shopId,
            OwnerUserId = _ownerId,
            Name = "SaveBite Bakery Central",
            Status = ShopStatus.Active
        };
    }

    // =========================================================================
    // UTCID01: Xem toàn bộ nhân viên (keyword = null) -> Thành công (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetShopStaffs_NoKeyword_ReturnsStaffList_Normal()
    {
        var staffUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "staff1@savebite.vn",
            FullName = "Nguyen Van Staff"
        };

        var staffList = new List<ShopStaffEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                UserId = staffUser.Id,
                User = staffUser,
                DisplayName = "Nguyen Van Staff",
                Nickname = "Staff A",
                Status = ShopStaffStatus.Active,
                JoinedAt = DateTime.UtcNow
            }
        };

        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _staffRepoMock.Setup(r => r.GetStaffsByShopIdPagedAsync(_shopId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((staffList, 1));

        var result = await _service.GetShopStaffsAsync(_ownerId, keyword: null, page: 1, pageSize: 10);

        PrintSuccess("UTCID01", "Staff list retrieved successfully without keyword.");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().DisplayName.Should().Be("Nguyen Van Staff");
    }

    // =========================================================================
    // UTCID02: Tìm kiếm nhân viên theo từ khóa (keyword = " Nguyen ") -> Thành công (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetShopStaffs_WithKeyword_ReturnsMatchingStaff_Normal()
    {
        var staffUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "staff.nguyen@savebite.vn",
            FullName = "Nguyen Van B"
        };

        var staffList = new List<ShopStaffEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                UserId = staffUser.Id,
                User = staffUser,
                DisplayName = "Nguyen Van B",
                Status = ShopStaffStatus.Active,
                JoinedAt = DateTime.UtcNow
            }
        };

        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        // Code service tự động trim: " Nguyen " -> "Nguyen"
        _staffRepoMock.Setup(r => r.GetStaffsByShopIdPagedAsync(_shopId, "Nguyen", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((staffList, 1));

        var result = await _service.GetShopStaffsAsync(_ownerId, keyword: " Nguyen ", page: 1, pageSize: 10);

        PrintSuccess("UTCID02", "Matching staff members retrieved successfully.");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.First().DisplayName.Should().Be("Nguyen Van B");
    }

    // =========================================================================
    // UTCID03: Shop chưa có nhân viên nào -> Trả về danh sách rỗng 0 phần tử (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetShopStaffs_Empty_ReturnsEmptyResult_Normal()
    {
        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _staffRepoMock.Setup(r => r.GetStaffsByShopIdPagedAsync(_shopId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ShopStaffEntity>(), 0));

        var result = await _service.GetShopStaffsAsync(_ownerId, keyword: null, page: 1, pageSize: 10);

        PrintSuccess("UTCID03", "Empty staff list returned.");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // =========================================================================
    // UTCID04: Chủ shop chưa đăng ký shop (Shop not found) -> 404 NotFound (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetShopStaffs_OwnerShopNotFound_ThrowsNotFound_Abnormal()
    {
        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        var act = () => _service.GetShopStaffsAsync(_ownerId, keyword: null, page: 1, pageSize: 10);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop not found.");
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
