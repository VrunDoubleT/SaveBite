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

namespace SaveBite.IntegrationTests.Features.StaffInvitations;

public class StaffInvitationsIntegrationTests : BaseIntegrationTest
{
    public StaffInvitationsIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<(User owner, Shop shop, User customer)> SetupOwnerAndCustomerAsync()
    {
        var owner = await AuthenticateAsStoreOwnerAsync(
            email: $"owner_{Guid.NewGuid():N}@test.com",
            fullName: "Owner Test");

        var shop = await DbContext.Shops.FirstAsync(s => s.OwnerUserId == owner.Id);

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"customer_{Guid.NewGuid():N}@test.com",
            FullName = "Customer Candidate",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        return (owner, shop, customer);
    }

    [Fact]
    public async Task GetMyInvitations_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Act
        var response = await Client.GetAsync("/api/staff-invitations/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InviteStaff_WhenCustomerCalls_ReturnsForbidden_403()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();
        var request = new InviteStaffRequest(Guid.NewGuid());

        // Act
        var response = await Client.PostAsJsonAsync("/api/staff-invitations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InviteStaff_WhenInvitingSelf_ReturnsBadRequest_400()
    {
        // Arrange
        var (owner, _, _) = await SetupOwnerAndCustomerAsync();
        var request = new InviteStaffRequest(owner.Id);

        // Act: Chủ shop tự mời chính mình làm nhân viên
        var response = await Client.PostAsJsonAsync("/api/staff-invitations", request);

        // Assert: 400 Bad Request ("Cannot invite yourself as staff")
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InviteStaff_WhenValidCustomer_CreatesPendingInvitation_200()
    {
        // Arrange
        var (owner, shop, customer) = await SetupOwnerAndCustomerAsync();
        var request = new InviteStaffRequest(customer.Id);

        // Act
        var response = await Client.PostAsJsonAsync("/api/staff-invitations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<StaffInvitationResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Status.Should().Be(nameof(StaffInvitationStatus.Pending));

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var invitationInDb = await DbContext.StaffInvitations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.ShopId == shop.Id && i.InvitedUserId == customer.Id);
        invitationInDb.Should().NotBeNull();
        invitationInDb!.Status.Should().Be(nameof(StaffInvitationStatus.Pending));
    }

    [Fact]
    public async Task GetMyInvitations_WhenCustomerCalls_ReturnsInvitationsList_200()
    {
        // Arrange
        var (owner, shop, customer) = await SetupOwnerAndCustomerAsync();

        var invitation = new StaffInvitation
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            InvitedUserId = customer.Id,
            InvitedBy = owner.Id,
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow
        };
        DbContext.StaffInvitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        // Đăng nhập với quyền Customer nhận lời mời
        AuthenticateAsCustomer(customer.Id, customer.Email);

        // Act
        var response = await Client.GetAsync("/api/staff-invitations/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<StaffInvitationResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Should().Contain(i => i.Id == invitation.Id);
    }

    [Fact]
    public async Task Accept_WhenPendingInvitationExists_AddsStaffMembershipAndReturns_200()
    {
        // Arrange
        var (owner, shop, customer) = await SetupOwnerAndCustomerAsync();

        var invitation = new StaffInvitation
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            InvitedUserId = customer.Id,
            InvitedBy = owner.Id,
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow
        };
        DbContext.StaffInvitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        AuthenticateAsCustomer(customer.Id, customer.Email);

        // Act: Chấp nhận lời mời
        var response = await Client.PostAsync($"/api/staff-invitations/{invitation.Id}/acceptances", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: Trạng thái lời mời thành Accepted & thêm bản ghi ShopStaff
        DbContext.ChangeTracker.Clear();
        var updatedInvitation = await DbContext.StaffInvitations.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invitation.Id);
        updatedInvitation.Should().NotBeNull();
        updatedInvitation!.Status.Should().Be(nameof(StaffInvitationStatus.Accepted));

        var membership = await DbContext.ShopStaffMembers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShopId == shop.Id && s.UserId == customer.Id);
        membership.Should().NotBeNull();
    }

    [Fact]
    public async Task Decline_WhenPendingInvitationExists_DeclinesInvitationAndReturns_200()
    {
        // Arrange
        var (owner, shop, customer) = await SetupOwnerAndCustomerAsync();

        var invitation = new StaffInvitation
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            InvitedUserId = customer.Id,
            InvitedBy = owner.Id,
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow
        };
        DbContext.StaffInvitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        AuthenticateAsCustomer(customer.Id, customer.Email);

        // Act: Từ chối lời mời
        var response = await Client.PostAsync($"/api/staff-invitations/{invitation.Id}/rejections", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: Lời mời thành Declined
        DbContext.ChangeTracker.Clear();
        var updatedInvitation = await DbContext.StaffInvitations.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invitation.Id);
        updatedInvitation.Should().NotBeNull();
        updatedInvitation!.Status.Should().Be(nameof(StaffInvitationStatus.Declined));
    }

    [Fact]
    public async Task RevokeInvitation_WhenOwnerRevokes_CancelsInvitationAndReturns_200()
    {
        // Arrange
        var (owner, shop, customer) = await SetupOwnerAndCustomerAsync();

        var invitation = new StaffInvitation
        {
            Id = Guid.NewGuid(),
            ShopId = shop.Id,
            InvitedUserId = customer.Id,
            InvitedBy = owner.Id,
            Status = nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow
        };
        DbContext.StaffInvitations.Add(invitation);
        await DbContext.SaveChangesAsync();

        // Act: Chủ shop thu hồi lời mời
        var response = await Client.DeleteAsync($"/api/staff-invitations/{invitation.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: Lời mời chuyển thành Cancelled
        DbContext.ChangeTracker.Clear();
        var updatedInvitation = await DbContext.StaffInvitations.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invitation.Id);
        updatedInvitation.Should().NotBeNull();
        updatedInvitation!.Status.Should().Be(nameof(StaffInvitationStatus.Cancelled));
    }

    private void AuthenticateAsCustomer(Guid userId, string email)
    {
        SetBearerToken(userId, email, UserRole.User);
    }
}
