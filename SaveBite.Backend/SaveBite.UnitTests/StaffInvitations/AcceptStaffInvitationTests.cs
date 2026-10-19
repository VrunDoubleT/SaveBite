using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
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

namespace SaveBite.Tests.StaffInvitations;

/// <summary>
/// Unit Tests for Feature: Accept Staff Invitation
/// Service: StaffInvitationService.AcceptInvitationAsync
/// </summary>
public class AcceptStaffInvitationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IStaffInvitationRepository> _invitationRepoMock = new();
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly StaffInvitationService _service;

    // Fixed Test Identifiers
    private readonly Guid _currentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _invitationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public AcceptStaffInvitationTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new StaffInvitationService(_invitationRepoMock.Object, _staffRepoMock.Object);

        var mockTransaction = new Mock<IDbContextTransaction>();
        _staffRepoMock.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);
    }

    private User CreateValidCandidateUser()
    {
        return new User
        {
            Id = _currentUserId,
            Email = "candidate@savebite.vn",
            FullName = "Nguyen Van Staff",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.User
        };
    }

    private Shop CreateValidShop()
    {
        return new Shop
        {
            Id = _shopId,
            Name = "SaveBite Central",
            Status = ShopStatus.Active
        };
    }

    private StaffInvitation CreateValidInvitation(User user, Shop shop, DateTime? invitedAt = null)
    {
        return new StaffInvitation
        {
            Id = _invitationId,
            ShopId = shop.Id,
            Shop = shop,
            InvitedUserId = user.Id,
            InvitedUser = user,
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = invitedAt ?? DateTime.UtcNow.AddDays(-1)
        };
    }

    // =========================================================================
    // UTCID01: Chấp nhận lời mời hợp lệ, còn hạn <= 3 ngày -> Thành công (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_AcceptInvitation_Valid_ShouldSucceed_Normal()
    {
        var user = CreateValidCandidateUser();
        var shop = CreateValidShop();
        var invitation = CreateValidInvitation(user, shop);

        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _staffRepoMock.Setup(r => r.UserOwnsApprovedShopAsync(_currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _staffRepoMock.Setup(r => r.HasActiveStaffMembershipPlatformWideAsync(_currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _staffRepoMock.Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopStaffEntity?)null);

        await _service.AcceptInvitationAsync(_currentUserId, _invitationId);

        PrintSuccess("UTCID01", "Invitation accepted and new ShopStaff membership created.");
        invitation.Status.Should().Be(nameof(StaffInvitationStatus.Accepted));
        _staffRepoMock.Verify(r => r.AddStaffAsync(It.Is<ShopStaffEntity>(s => s.ShopId == _shopId && s.UserId == _currentUserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Lời mời không tồn tại trong DB -> 404 NotFound (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_AcceptInvitation_NotFound_ThrowsNotFound_Abnormal()
    {
        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffInvitation?)null);

        var act = () => _service.AcceptInvitationAsync(_currentUserId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
    }

    // =========================================================================
    // UTCID03: Lời mời gửi từ 5 ngày trước (> 3 ngày) -> Tự động hủy và ném 400 BadRequest (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_AcceptInvitation_Expired_AutoCancelsAndThrowsBadRequest_Abnormal()
    {
        var user = CreateValidCandidateUser();
        var shop = CreateValidShop();
        var invitation = CreateValidInvitation(user, shop, invitedAt: DateTime.UtcNow.AddDays(-5));

        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var act = () => _service.AcceptInvitationAsync(_currentUserId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvitationInvalidOrExpired);
        invitation.Status.Should().Be(nameof(StaffInvitationStatus.Cancelled));
    }

    // =========================================================================
    // UTCID04: Người dùng đã là nhân viên active ở một shop khác -> 409 Conflict (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID04_AcceptInvitation_AlreadyActiveStaffElsewhere_ThrowsConflict_Abnormal()
    {
        var user = CreateValidCandidateUser();
        var shop = CreateValidShop();
        var invitation = CreateValidInvitation(user, shop);

        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _staffRepoMock.Setup(r => r.UserOwnsApprovedShopAsync(_currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _staffRepoMock.Setup(r => r.HasActiveStaffMembershipPlatformWideAsync(_currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _service.AcceptInvitationAsync(_currentUserId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.AlreadyActiveStaff);
    }

    // =========================================================================
    // UTCID05: Người dùng đã sở hữu một cửa hàng được duyệt -> 400 BadRequest (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID05_AcceptInvitation_OwnsApprovedShop_ThrowsBadRequest_Abnormal()
    {
        var user = CreateValidCandidateUser();
        var shop = CreateValidShop();
        var invitation = CreateValidInvitation(user, shop);

        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _staffRepoMock.Setup(r => r.UserOwnsApprovedShopAsync(_currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _service.AcceptInvitationAsync(_currentUserId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvalidStaffCandidate);
    }

    // =========================================================================
    // UTCID06: Người này từng bị chính shop này kỷ luật/đình chỉ -> 403 Forbidden (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID06_AcceptInvitation_StaffSuspendedAtThisShop_ThrowsForbidden_Abnormal()
    {
        var user = CreateValidCandidateUser();
        var shop = CreateValidShop();
        var invitation = CreateValidInvitation(user, shop);
        var suspendedStaff = new ShopStaffEntity
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            UserId = _currentUserId,
            Status = ShopStaffStatus.Suspended
        };

        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _staffRepoMock.Setup(r => r.UserOwnsApprovedShopAsync(_currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _staffRepoMock.Setup(r => r.HasActiveStaffMembershipPlatformWideAsync(_currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _staffRepoMock.Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedStaff);

        var act = () => _service.AcceptInvitationAsync(_currentUserId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID06", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.StaffSuspended);
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
