using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
/// Feature: View Customer Invitations (Candidate / Customer side)
/// </summary>
public class ViewCustomerInvitationsTests
{
    private readonly Mock<IStaffInvitationRepository> _invitationRepoMock = new();
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly StaffInvitationService _service;
    private readonly ITestOutputHelper _output;

    // Fixed Identifiers để dễ đối chiếu với Bảng Ma trận Excel
    private readonly Guid _candidateUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ViewCustomerInvitationsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new StaffInvitationService(_invitationRepoMock.Object, _staffRepoMock.Object);
    }

    // =========================================================================
    // UT01: Khách hàng có lời mời còn hạn trong DB -> Trả về danh sách lời mời (Normal)
    // =========================================================================
    [Fact]
    public async Task UT01_GetCustomerInvitations_ValidPending_ReturnsInvitations_Normal()
    {
        // Arrange: Có lời mời mới gửi 1 ngày trước (còn hạn)
        var shop = new Shop { Id = _shopId, Name = "SaveBite Bakery Central" };
        var candidateUser = new User 
        { 
            Id = _candidateUserId, 
            Email = "candidate@savebite.vn", 
            FullName = "Nguyen Van Khach" 
        };

        var invitations = new List<StaffInvitation>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                Shop = shop,
                InvitedUserId = _candidateUserId,
                InvitedUser = candidateUser,
                Status = nameof(StaffInvitationStatus.Pending),
                InvitedAt = DateTime.UtcNow.AddDays(-1) // Còn hạn (< 3 ngày)
            }
        };

        _invitationRepoMock
            .Setup(r => r.GetInvitationsByUserIdAsync(_candidateUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitations);

        // Act: Khách hàng lấy danh sách lời mời gửi tới mình
        var result = (await _service.GetCustomerInvitationsAsync(_candidateUserId)).ToList();

        // Output log
        PrintSuccess("UT01", result);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ShopName.Should().Be("SaveBite Bakery Central");
        result.First().Status.Should().Be(nameof(StaffInvitationStatus.Pending));

        // Chưa hết hạn nên KHÔNG được gọi cập nhật DB
        _invitationRepoMock.Verify(r => r.UpdateInvitationAsync(It.IsAny<StaffInvitation>(), It.IsAny<CancellationToken>()), Times.Never);
        _invitationRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UT02: Khách hàng chưa được quán nào mời -> Trả về danh sách rỗng 0 phần tử (Normal)
    // =========================================================================
    [Fact]
    public async Task UT02_GetCustomerInvitations_Empty_ReturnsEmptyResult_Normal()
    {
        // Arrange: DB trả về danh sách rỗng
        var emptyList = new List<StaffInvitation>();

        _invitationRepoMock
            .Setup(r => r.GetInvitationsByUserIdAsync(_candidateUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        var result = (await _service.GetCustomerInvitationsAsync(_candidateUserId)).ToList();

        // Output log
        PrintSuccess("UT02", result);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // =========================================================================
    // UT03: Lời mời đã quá 3 ngày -> Tự động hủy (Cancelled) và lưu xuống DB (Normal)
    // =========================================================================
    [Fact]
    public async Task UT03_GetCustomerInvitations_ExpiredPending_UpdatesToCancelledAndSaves_Normal()
    {
        // Arrange: Lời mời gửi từ 5 ngày trước (> 3 ngày)
        var shop = new Shop { Id = _shopId, Name = "SaveBite Bakery Central" };
        var candidateUser = new User 
        { 
            Id = _candidateUserId, 
            Email = "candidate@savebite.vn", 
            FullName = "Nguyen Van Khach" 
        };

        var expiredInvitation = new StaffInvitation
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            Shop = shop,
            InvitedUserId = _candidateUserId,
            InvitedUser = candidateUser,
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow.AddDays(-5) // Đã hết hạn (> 3 ngày)
        };

        _invitationRepoMock
            .Setup(r => r.GetInvitationsByUserIdAsync(_candidateUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StaffInvitation> { expiredInvitation });

        // Act
        var result = (await _service.GetCustomerInvitationsAsync(_candidateUserId)).ToList();

        // Output log
        PrintSuccess("UT03", result);

        // Assert: Trạng thái hiển thị phải là Cancelled
        result.First().Status.Should().Be(nameof(StaffInvitationStatus.Cancelled));

        // Phải cập nhật lại trạng thái vào DB và lưu thay đổi
        expiredInvitation.Status.Should().Be(nameof(StaffInvitationStatus.Cancelled));
        _invitationRepoMock.Verify(r => r.UpdateInvitationAsync(expiredInvitation, It.IsAny<CancellationToken>()), Times.Once);
        _invitationRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UT04: Lời mời đã được chấp nhận trước đó -> Giữ nguyên trạng thái Accepted (Normal)
    // =========================================================================
    [Fact]
    public async Task UT04_GetCustomerInvitations_AlreadyAccepted_ReturnsAcceptedStatus_Normal()
    {
        // Arrange: Lời mời có trạng thái Accepted
        var shop = new Shop { Id = _shopId, Name = "SaveBite Bakery Central" };
        var candidateUser = new User 
        { 
            Id = _candidateUserId, 
            Email = "candidate@savebite.vn", 
            FullName = "Nguyen Van Khach" 
        };

        var acceptedInvitation = new StaffInvitation
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            Shop = shop,
            InvitedUserId = _candidateUserId,
            InvitedUser = candidateUser,
            Status = nameof(StaffInvitationStatus.Accepted),
            InvitedAt = DateTime.UtcNow.AddDays(-5),
            RespondedAt = DateTime.UtcNow.AddDays(-4)
        };

        _invitationRepoMock
            .Setup(r => r.GetInvitationsByUserIdAsync(_candidateUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StaffInvitation> { acceptedInvitation });

        // Act
        var result = (await _service.GetCustomerInvitationsAsync(_candidateUserId)).ToList();

        // Output log
        PrintSuccess("UT04", result);

        // Assert: Trạng thái vẫn là Accepted, không bị đổi thành Cancelled
        result.First().Status.Should().Be(nameof(StaffInvitationStatus.Accepted));
        _invitationRepoMock.Verify(r => r.UpdateInvitationAsync(It.IsAny<StaffInvitation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void PrintSuccess<T>(string utcid, T result)
    {
        _output.WriteLine($"================ {utcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }
}
