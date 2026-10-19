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
/// Unit Tests for Feature: Remove Staff Member
/// Service: ShopStaffService.RemoveStaffAsync
/// Function Code: STAFF-010
/// </summary>
public class RemoveStaffMemberTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly ShopStaffService _service;

    // Fixed Identifiers
    private readonly Guid _ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _staffId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Shop _ownerShop;

    public RemoveStaffMemberTests(ITestOutputHelper output)
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
    // UTCID01: Xóa nhân viên hợp lệ đang hoạt động -> Thành công (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_RemoveStaff_Valid_ShouldSucceed_Normal()
    {
        var staff = new ShopStaffEntity
        {
            Id = _staffId,
            ShopId = _shopId,
            Status = ShopStaffStatus.Active
        };

        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);
        _staffRepoMock.Setup(r => r.GetStaffByIdAsync(_staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        await _service.RemoveStaffAsync(_ownerId, _staffId);

        PrintSuccess("UTCID01", "Staff member removed successfully.");
        staff.Status.Should().Be(ShopStaffStatus.Removed);
        _staffRepoMock.Verify(r => r.UpdateStaffAsync(staff, It.IsAny<CancellationToken>()), Times.Once);
        _staffRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Nhân viên không tồn tại trong DB -> 404 NotFound (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_RemoveStaff_NotFound_ThrowsNotFound_Abnormal()
    {
        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);
        _staffRepoMock.Setup(r => r.GetStaffByIdAsync(_staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopStaffEntity?)null);

        var act = () => _service.RemoveStaffAsync(_ownerId, _staffId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.StaffNotFound);
    }

    // =========================================================================
    // UTCID03: Nhân viên đã bị xóa từ trước (Status == Removed) -> 404 NotFound (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_RemoveStaff_AlreadyRemoved_ThrowsNotFound_Abnormal()
    {
        var staff = new ShopStaffEntity
        {
            Id = _staffId,
            ShopId = _shopId,
            Status = ShopStaffStatus.Removed
        };

        _staffRepoMock.Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);
        _staffRepoMock.Setup(r => r.GetStaffByIdAsync(_staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var act = () => _service.RemoveStaffAsync(_ownerId, _staffId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.StaffNotFound);
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
