using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.BackgroundJobs.Interfaces;
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
/// Unit Tests for Feature: Decline Staff Invitation
/// Service: StaffInvitationService.DeclineInvitationAsync
/// </summary>
public class DeclineStaffInvitationTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IStaffInvitationRepository> _invitationRepoMock = new();
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly StaffInvitationService _service;

    // Fixed Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _invitationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _ownerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public DeclineStaffInvitationTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new StaffInvitationService(_invitationRepoMock.Object, _staffRepoMock.Object, new Mock<IStaffInvitationScheduler>().Object, new StaffInvitationNotificationService(_invitationRepoMock.Object, new Mock<INotificationQueue>().Object), TestDbContextFactory.Create(), new Mock<IRealtimePublisher>().Object);
        _invitationRepoMock.Setup(r => r.TryTransitionPendingAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    // =========================================================================
    // UTCID01: Từ chối lời mời hợp lệ (Pending, còn hạn <= 3 ngày) -> Thành công (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_DeclineInvitation_ValidPending_ShouldSucceed_Normal()
    {
        var invitation = new StaffInvitation
        {
            Id = _invitationId,
            InvitedUserId = _userId,
            ShopId = _shopId,
            Shop = new Shop { Id = _shopId, OwnerUserId = _ownerId },
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow.AddDays(-1) // Còn hạn (1 ngày trước)
        };

        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        await _service.DeclineInvitationAsync(_userId, _invitationId);

        PrintSuccess("UTCID01", "Invitation declined successfully.");
        _invitationRepoMock.Verify(r => r.TryTransitionPendingAsync(_invitationId, nameof(StaffInvitationStatus.Declined), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Lời mời không tồn tại trong DB -> 404 NotFound (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_DeclineInvitation_NotFound_ThrowsNotFound_Abnormal()
    {
        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffInvitation?)null);

        var act = () => _service.DeclineInvitationAsync(_userId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvitationNotFound);
    }

    // =========================================================================
    // UTCID03: Lời mời gửi từ 5 ngày trước (> 3 ngày) -> Tự động hủy và ném 400 BadRequest (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_DeclineInvitation_Expired_AutoCancelsAndThrowsBadRequest_Abnormal()
    {
        var invitation = new StaffInvitation
        {
            Id = _invitationId,
            InvitedUserId = _userId,
            ShopId = _shopId,
            Shop = new Shop { Id = _shopId, OwnerUserId = _ownerId },
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow.AddDays(-5) // Đã quá hạn 5 ngày
        };

        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var act = () => _service.DeclineInvitationAsync(_userId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvitationInvalidOrExpired);
        _invitationRepoMock.Verify(r => r.TryTransitionPendingAsync(_invitationId, nameof(StaffInvitationStatus.Expired), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID04: Lời mời không ở trạng thái Pending (Đã Accepted trước đó) -> 400 BadRequest (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID04_DeclineInvitation_NotPending_ThrowsBadRequest_Abnormal()
    {
        var invitation = new StaffInvitation
        {
            Id = _invitationId,
            InvitedUserId = _userId,
            Status = nameof(StaffInvitationStatus.Accepted),
            InvitedAt = DateTime.UtcNow.AddDays(-1)
        };

        _invitationRepoMock.Setup(r => r.GetInvitationByIdAsync(_invitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var act = () => _service.DeclineInvitationAsync(_userId, _invitationId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvitationInvalidOrExpired);
        ex.Which.Message.Should().Be("Only pending invitations can be declined");
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
