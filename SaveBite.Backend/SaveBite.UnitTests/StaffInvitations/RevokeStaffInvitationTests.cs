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

namespace SaveBite.Tests.StaffInvitations;

/// <summary>
/// Unit Tests cho chức năng RevokeInvitationAsync (Thu hồi lời mời nhân viên)
/// Mã chức năng: STAFF-002
/// </summary>
public class RevokeStaffInvitationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IStaffInvitationRepository> _invitationRepoMock = new();
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly StaffInvitationService _service;

    // Fixed Test Data
    private readonly Guid _ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _otherShopId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _invitationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Shop _ownerShop;

    public RevokeStaffInvitationTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new StaffInvitationService(_invitationRepoMock.Object, _staffRepoMock.Object);

        _ownerShop = new Shop
        {
            Id = _shopId,
            OwnerUserId = _ownerId,
            Name = "SaveBite Flagship Store",
            Status = ShopStatus.Active
        };
    }

    // =========================================================
    // UT01: Thu hồi lời mời hợp lệ (Pending, còn hạn 3 ngày) -> Thành công (Normal)
    // =========================================================
    [Fact]
    public async Task UT01_RevokeInvitation_ValidPendingInvitation_ShouldSucceed_Normal()
    {
        // Arrange
        var invitation = new StaffInvitation
        {
            Id = _invitationId,
            ShopId = _shopId,
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow.AddHours(-2) // Còn hạn (mới mời 2 tiếng trước)
        };

        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        // Act
        await _service.RevokeInvitationAsync(_ownerId, _invitationId);

        // Output log
        PrintSuccess("UT01", "Invitation successfully revoked and marked as Cancelled.");

        // Assert
        invitation.Status.Should().Be(nameof(StaffInvitationStatus.Cancelled));
        invitation.RespondedAt.Should().NotBeNull();
        _invitationRepoMock.Verify(r => r.UpdateInvitationAsync(invitation, It.IsAny<CancellationToken>()), Times.Once);
        _invitationRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================
    // UT02: Shop của Chủ quán không tồn tại -> 404 NotFound (Abnormal)
    // =========================================================
    [Fact]
    public async Task UT02_RevokeInvitation_OwnerShopNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act & Assert
        var act = () => _service.RevokeInvitationAsync(_ownerId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UT02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Shop not found.");
    }

    // =========================================================
    // UT03: Lời mời không tồn tại trong hệ thống -> 404 NotFound (Abnormal)
    // =========================================================
    [Fact]
    public async Task UT03_RevokeInvitation_InvitationNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffInvitation?)null);

        // Act & Assert
        var act = () => _service.RevokeInvitationAsync(_ownerId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UT03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
    }

    // =========================================================
    // UT04: Lời mời thuộc về shop khác (Cross-shop security) -> 404 NotFound (Abnormal)
    // =========================================================
    [Fact]
    public async Task UT04_RevokeInvitation_CrossShopInvitation_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var invitation = new StaffInvitation
        {
            Id = _invitationId,
            ShopId = _otherShopId, // Khác ShopId của Owner
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow
        };

        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        // Act & Assert
        var act = () => _service.RevokeInvitationAsync(_ownerId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UT04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
    }

    // =========================================================
    // UT05: Lời mời không ở trạng thái Pending (Đã Accepted/Declined) -> 400 BadRequest (Abnormal)
    // =========================================================
    [Fact]
    public async Task UT05_RevokeInvitation_NotPendingStatus_ThrowsBadRequest_Abnormal()
    {
        // Arrange: Lời mời đã được chấp nhận trước đó
        var invitation = new StaffInvitation
        {
            Id = _invitationId,
            ShopId = _shopId,
            Status = nameof(StaffInvitationStatus.Accepted),
            InvitedAt = DateTime.UtcNow.AddHours(-1)
        };

        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        // Act & Assert
        var act = () => _service.RevokeInvitationAsync(_ownerId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UT05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvitationInvalidOrExpired);
        ex.Which.Message.Should().Be("Only pending invitations can be revoked");
    }

    // =========================================================
    // UT06: Lời mời Pending nhưng đã quá hạn 3 ngày -> Tự động hủy và ném 400 BadRequest (Abnormal)
    // =========================================================
    [Fact]
    public async Task UT06_RevokeInvitation_ExpiredInvitation_AutoCancelsAndThrowsBadRequest_Abnormal()
    {
        // Arrange: Lời mời đã gửi từ 4 ngày trước (> 3 ngày)
        var invitation = new StaffInvitation
        {
            Id = _invitationId,
            ShopId = _shopId,
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow.AddDays(-4)
        };

        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        // Act & Assert
        var act = () => _service.RevokeInvitationAsync(_ownerId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UT06", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvitationInvalidOrExpired);
        ex.Which.Message.Should().Be("Invitation has already expired and has been cancelled");

        // Verify: Hệ thống đã tự động cập nhật Status = Cancelled và lưu vào DB
        invitation.Status.Should().Be(nameof(StaffInvitationStatus.Cancelled));
        invitation.RespondedAt.Should().NotBeNull();
        _invitationRepoMock.Verify(r => r.UpdateInvitationAsync(invitation, It.IsAny<CancellationToken>()), Times.Once);
        _invitationRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void PrintSuccess(string utcid, string message)
    {
        _output.WriteLine($"================ {utcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }

    private void PrintException(string utcid, AppException ex)
    {
        _output.WriteLine($"================ {utcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"ERROR_CODE: {ex.ErrorCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }
}
