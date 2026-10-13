using FluentAssertions;
using Moq;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;
using ShopStaffEntity = SaveBite.Backend.Models.Entities.ShopStaff;

namespace SaveBite.Tests.ShopStaffManagement;

/// <summary>
/// Unit Tests for Feature: View Associated Shops (Xem danh sách quán nhân viên trực thuộc)
/// Service: ShopStaffService.GetAssociatedShopsAsync
/// Function Code: STAFF-013
/// </summary>
public class ViewAssociatedShopsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly ShopStaffService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ViewAssociatedShopsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ShopStaffService(_staffRepoMock.Object);
    }

    // =========================================================================
    // UTCID01: Nhân viên có quán đang làm việc -> Trả về thông tin quán (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetAssociatedShops_HasMembership_ReturnsShop_Normal()
    {
        var shop = new Shop { Id = _shopId, Name = "SaveBite Bakery Central" };
        var memberships = new List<ShopStaffEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                Shop = shop,
                UserId = _userId,
                Status = ShopStaffStatus.Active,
                JoinedAt = DateTime.UtcNow
            }
        };

        _staffRepoMock.Setup(r => r.GetShopsByStaffUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberships);

        var result = (await _service.GetAssociatedShopsAsync(_userId)).ToList();

        PrintSuccess("UTCID01", $"Retrieved {result.Count} associated shop successfully.");
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().ShopId.Should().Be(_shopId);
        result.First().ShopName.Should().Be("SaveBite Bakery Central");
        result.First().Status.Should().Be(ShopStaffStatus.Active);
    }

    // =========================================================================
    // UTCID02: Nhân viên chưa tham gia quán nào -> Trả về danh sách rỗng 0 phần tử (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetAssociatedShops_EmptyMemberships_ReturnsEmptyList_Normal()
    {
        _staffRepoMock.Setup(r => r.GetShopsByStaffUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShopStaffEntity>());

        var result = (await _service.GetAssociatedShopsAsync(_userId)).ToList();

        PrintSuccess("UTCID02", "Retrieved empty associated shops list successfully.");
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

  
    private void PrintSuccess(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }

    private void PrintException(string tcid, string errorMessage)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: ERROR");
        _output.WriteLine($"MESSAGE: {errorMessage}");
        _output.WriteLine("========================================");
    }
}
