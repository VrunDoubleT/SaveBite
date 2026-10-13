using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
/// Feature: View Shop and Staff Info
/// </summary>
public class ViewShopAndStaffInfoTests
{
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly ShopStaffService _service;
    private readonly ITestOutputHelper _output;

    // Fixed Identifiers để dễ đối chiếu với Bảng Ma trận Excel
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _staffUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public ViewShopAndStaffInfoTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopStaffService(_staffRepoMock.Object);
    }

    [Fact]
    public async Task UT01_GetShopAndStaffInfo_ActiveStaff_ShouldReturnInfo_Normal()
    {
        // Arrange
        var user = new User 
        { 
            Id = _staffUserId, 
            Email = "staff.alex@savebite.vn",
            FullName = "Alex Staff"
        };

        var shop = new Shop
        {
            Id = _shopId,
            Name = "SaveBite Central",
            Description = "Great food",
            Status = ShopStatus.Active
        };

        var staff = new ShopStaffEntity
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            Shop = shop,
            UserId = _staffUserId,
            User = user,
            DisplayName = "Staff Alex",
            Status = ShopStaffStatus.Active,
            JoinedAt = DateTime.UtcNow
        };

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        // Act
        var result = await _service.GetShopAndStaffInfoAsync(_staffUserId, _shopId);

        // Output log
        PrintSuccess("UT01", result);

        // Assert
        result.Should().NotBeNull();
        result.Shop.Id.Should().Be(_shopId);
        result.Shop.Name.Should().Be("SaveBite Central");
        result.Staff.DisplayName.Should().Be("Staff Alex");
    }

    [Fact]
    public async Task UT02_GetShopAndStaffInfo_NotStaff_ShouldThrowForbidden_Abnormal()
    {
        // Arrange: Không tìm thấy quan hệ nhân viên trong DB (null)
        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopStaffEntity?)null);

        // Act & Assert
        var act = () => _service.GetShopAndStaffInfoAsync(_staffUserId, _shopId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UT02", ex.Which);

        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("You are not an active staff member of this shop");
    }

    [Fact]
    public async Task UT03_GetShopAndStaffInfo_SuspendedStaff_ShouldThrowForbidden_Abnormal()
    {
        // Arrange: Nhân viên bị khóa (Suspended)
        var staff = new ShopStaffEntity
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            UserId = _staffUserId,
            Status = ShopStaffStatus.Suspended
        };

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(_shopId, _staffUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        // Act & Assert
        var act = () => _service.GetShopAndStaffInfoAsync(_staffUserId, _shopId);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UT03", ex.Which);

        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("You are not an active staff member of this shop");
    }

    // Helper methods in log giống hệt InviteStaffTests
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
