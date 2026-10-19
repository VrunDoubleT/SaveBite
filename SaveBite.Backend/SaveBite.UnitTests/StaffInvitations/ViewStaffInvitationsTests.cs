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
/// Feature: View Shop Invitations (Owner side)
/// </summary>
public class ViewStaffInvitationsTests
{
    private readonly Mock<IStaffInvitationRepository> _invitationRepoMock = new();
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly StaffInvitationService _service;
    private readonly ITestOutputHelper _output;

    // Fixed Identifiers để dễ đối chiếu với Bảng Ma trận Excel
    private readonly Guid _ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Shop _ownerShop;

    public ViewStaffInvitationsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new StaffInvitationService(_invitationRepoMock.Object, _staffRepoMock.Object);

        _ownerShop = new Shop
        {
            Id = _shopId,
            OwnerUserId = _ownerId,
            Name = "SaveBite Bakery Central",
            Status = ShopStatus.Active
        };
    }

    // =========================================================
    // UT01: Có lời mời trong DB -> Trả về danh sách lời mời (Normal)
    // =========================================================
    [Fact]
    public async Task UT01_GetShopInvitations_HasItems_ReturnsInvitations_Normal()
    {
        // Arrange
        var items = new List<StaffInvitation>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                Shop = _ownerShop,
                InvitedUserId = Guid.NewGuid(),
                InvitedUser = new User { Id = Guid.NewGuid(), Email = "candidate1@savebite.vn", FullName = "Candidate 1" },
                Status = nameof(StaffInvitationStatus.Pending),
                InvitedAt = DateTime.UtcNow.AddDays(-1) // Còn hạn (mới gửi 1 ngày)
            }
        };

        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.GetInvitationsByShopIdPagedAsync(_shopId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        // Act
        var result = await _service.GetShopInvitationsAsync(_ownerId, page: 1, pageSize: 10);

        // Output log
        PrintSuccess("UT01", result);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().ShopId.Should().Be(_shopId);
        result.Items.First().InvitedUserEmail.Should().Be("candidate1@savebite.vn");
        result.Items.First().Status.Should().Be(nameof(StaffInvitationStatus.Pending));
    }

    // =========================================================
    // UT02: Không có lời mời nào (Rỗng) -> Trả về danh sách rỗng 0 phần tử (Normal)
    // =========================================================
    [Fact]
    public async Task UT02_GetShopInvitations_Empty_ReturnsEmptyResult_Normal()
    {
        // Arrange
        var emptyList = new List<StaffInvitation>();

        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.GetInvitationsByShopIdPagedAsync(_shopId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((emptyList, 0));

        // Act
        var result = await _service.GetShopInvitationsAsync(_ownerId, page: 1, pageSize: 10);

        // Output log
        PrintSuccess("UT02", result);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // =========================================================
    // UT03: Lời mời đã hết hạn (> 3 ngày) -> Tự động chuyển thành Cancelled (Normal)
    // =========================================================
    [Fact]
    public async Task UT03_GetShopInvitations_ExpiredItem_ReturnsCancelledStatus_Normal()
    {
        // Arrange
        var expiredItem = new StaffInvitation
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            Shop = _ownerShop,
            InvitedUserId = Guid.NewGuid(),
            InvitedUser = new User { Id = Guid.NewGuid(), Email = "expired@savebite.vn", FullName = "Expired User" },
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow.AddDays(-5) // Đã quá hạn 3 ngày
        };

        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.GetInvitationsByShopIdPagedAsync(_shopId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<StaffInvitation> { expiredItem }, 1));

        // Act
        var result = await _service.GetShopInvitationsAsync(_ownerId, page: 1, pageSize: 10);

        // Output log
        PrintSuccess("UT03", result);

        // Assert: Lời mời quá hạn phải được map thành Cancelled
        result.Items.First().Status.Should().Be("Expired");
    }

    // =========================================================
    // UT04: Chủ quán chưa đăng ký shop -> Ném lỗi 404 NotFound (Abnormal)
    // =========================================================
    [Fact]
    public async Task UT04_GetShopInvitations_OwnerShopNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act & Assert
        var act = () => _service.GetShopInvitationsAsync(_ownerId, page: 1, pageSize: 10);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UT04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helper methods in log giống chuẩn dự án
    private void PrintSuccess<T>(string utcid, T result)
    {
        _output.WriteLine($"================ {utcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }

    private void PrintException(string utcid, AppException ex)
    {
        _output.WriteLine($"================ {utcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }
}
