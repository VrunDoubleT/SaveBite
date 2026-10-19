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
/// Unit Tests for Feature: Leave Shop (Nhân viên tự rời khỏi quán)
/// Service: ShopStaffService.LeaveShopAsync
/// Function Code: STAFF-011
/// </summary>
public class LeaveShopTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly ShopStaffService _service;

    // Fixed Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _staffId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public LeaveShopTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopStaffService(_staffRepoMock.Object);
    }

    // =========================================================================
    // UTCID01: Nhân viên Active rời quán, không còn làm quán khác -> Chuyển Removed, Role về User (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_LeaveShop_ActiveStaff_NoOtherMembership_DemoteRole_Normal()
    {
        var user = new User { Id = _userId, Role = UserRole.Staff };
        var staff = new ShopStaffEntity
        {
            Id = _staffId,
            ShopId = _shopId,
            UserId = _userId,
            User = user,
            Status = ShopStaffStatus.Active
        };

        _staffRepoMock.Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);
        _staffRepoMock.Setup(r => r.HasOtherStaffMembershipAsync(_userId, _staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _service.LeaveShopAsync(_userId, _shopId);

        PrintSuccess("UTCID01", "Left shop successfully. Staff membership removed and role demoted to User.");
        staff.Status.Should().Be(ShopStaffStatus.Removed);
        user.Role.Should().Be(UserRole.User);
        _staffRepoMock.Verify(r => r.UpdateStaffAsync(staff, It.IsAny<CancellationToken>()), Times.Once);
        _staffRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

   

    // =========================================================================
    // UTCID03: Không phải nhân viên của quán này -> 404 NotFound (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_LeaveShop_NotStaff_ThrowsNotFound_Abnormal()
    {
        _staffRepoMock.Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopStaffEntity?)null);

        var act = () => _service.LeaveShopAsync(_userId, _shopId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.StaffNotFound);
        ex.Which.Message.Should().Be("You are not a staff member of this shop");
    }

    // =========================================================================
    // UTCID04: Nhân viên đã rời quán từ trước (Status == Removed) -> 400 BadRequest (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID04_LeaveShop_AlreadyLeft_ThrowsBadRequest_Abnormal()
    {
        var staff = new ShopStaffEntity
        {
            Id = _staffId,
            ShopId = _shopId,
            UserId = _userId,
            Status = ShopStaffStatus.Removed
        };

        _staffRepoMock.Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var act = () => _service.LeaveShopAsync(_userId, _shopId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvalidStaffStatus);
        ex.Which.Message.Should().Be("You have already left this shop");
    }

    // =========================================================================
    // UTCID05: Nhân viên đang bị kỷ luật/đình chỉ (Status == Suspended) -> 403 Forbidden (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID05_LeaveShop_SuspendedStaff_ThrowsForbidden_Abnormal()
    {
        var staff = new ShopStaffEntity
        {
            Id = _staffId,
            ShopId = _shopId,
            UserId = _userId,
            Status = ShopStaffStatus.Suspended
        };

        _staffRepoMock.Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        var act = () => _service.LeaveShopAsync(_userId, _shopId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.StaffSuspended);
        ex.Which.Message.Should().Be("This staff membership has been suspended by the shop");
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
