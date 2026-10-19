using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;

namespace SaveBite.IntegrationTests.Features.ShopStaffFeatures;

public class ShopStaffIntegrationTests : BaseIntegrationTest
{
    public ShopStaffIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User owner, Shop shop, User staffUser, ShopStaff staffMember)> SetupOwnerAndStaffAsync()
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@test.com",
            fullName: "Owner Staff Manager");

        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);

        var staffUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"staff_{Guid.NewGuid():N}@test.com",
            FullName = "Staff Member",
            PasswordHash = "dummyhash",
            Role = UserRole.Staff,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(staffUser);

        var staffMember = new ShopStaff
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            UserId = staffUser.Id,
            DisplayName = "Staff Ban Hang",
            Nickname = "Ti",
            Status = ShopStaffStatus.Active,
            JoinedAt = DateTime.UtcNow
        };
        DbContext.ShopStaffMembers.Add(staffMember);
        await DbContext.SaveChangesAsync();

        return (owner, shop, staffUser, staffMember);
    }

    [Fact]
    public async Task GetStaffs_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Act
        var response = await Client.GetAsync("/api/shop-staff");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStaffs_WhenCustomerCalls_ReturnsForbidden_403()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/shop-staff");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetStaffs_WhenStoreOwnerCalls_ReturnsStaffList_200()
    {
        // Arrange
        var (_, _, _, staffMember) = await SetupOwnerAndStaffAsync();

        // Act
        var response = await Client.GetAsync("/api/shop-staff");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ShopStaffResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().Contain(s => s.Id == staffMember.Id);
    }

    [Fact]
    public async Task UpdateStaff_WhenValidRequest_UpdatesDatabaseAndReturns_200()
    {
        // Arrange
        var (_, _, _, staffMember) = await SetupOwnerAndStaffAsync();

        var updateRequest = new UpdateStaffInfoRequest(
            DisplayName: "Staff Thu Ngan Moi",
            StaffNickname: "Teo",
            Status: ShopStaffStatus.Active
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/shop-staff/{staffMember.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var updatedInDb = await DbContext.ShopStaffMembers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == staffMember.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.DisplayName.Should().Be("Staff Thu Ngan Moi");
        updatedInDb.Nickname.Should().Be("Teo");
    }

    [Fact]
    public async Task RemoveStaff_WhenStoreOwnerRemoves_SoftDeletesInDatabaseAndReturns_200()
    {
        // Arrange
        var (_, _, _, staffMember) = await SetupOwnerAndStaffAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/shop-staff/{staffMember.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: Soft delete chuyển Status thành Removed
        DbContext.ChangeTracker.Clear();
        var staffInDb = await DbContext.ShopStaffMembers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == staffMember.Id);
        staffInDb.Should().NotBeNull();
        staffInDb!.Status.Should().Be(ShopStaffStatus.Removed);
    }

    [Fact]
    public async Task GetAssociatedShops_WhenStaffCalls_ReturnsAssociatedShops_200()
    {
        // Arrange
        var (_, shop, staffUser, _) = await SetupOwnerAndStaffAsync();

        // Authenticate với tài khoản của Staff
        SetBearerToken(staffUser.Id, staffUser.Email, UserRole.Staff);

        // Act
        var response = await Client.GetAsync("/api/shop-staff/me/shops");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<AssociatedShopResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Should().Contain(s => s.ShopId == shop.Id);
    }
}
