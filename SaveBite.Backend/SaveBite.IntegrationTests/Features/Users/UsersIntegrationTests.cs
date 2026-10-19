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

namespace SaveBite.IntegrationTests.Features.Users;

public class UsersIntegrationTests : BaseIntegrationTest
{
    public UsersIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMe_WhenAuthenticatedAsCustomer_ReturnsProfile_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync(
            email: $"cust_{Guid.NewGuid():N}@test.com",
            fullName: "Nguyen Van Test");

        // Act
        var response = await Client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentUserResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Email.Should().Be(customer.Email);
        body.Data.FullName.Should().Be("Nguyen Van Test");
    }

    [Fact]
    public async Task GetMe_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Act (Không gửi Authorization header)
        var response = await Client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WhenValidRequest_UpdatesDatabaseAndReturns_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync(
            email: $"cust_{Guid.NewGuid():N}@test.com",
            fullName: "Old Name");

        var updateRequest = new UpdateUserProfileRequest
        {
            FullName = "New Updated Name",
            Phone = "0999888777"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/users/me", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var updatedInDb = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == customer.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.FullName.Should().Be("New Updated Name");
        updatedInDb.Phone.Should().Be("0999888777");
    }

    [Fact]
    public async Task GetUsers_WhenCustomerCallsAdminEndpoint_ReturnsForbidden_403()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert (Customer không có quyền truy cập [AdminAccess])
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_WhenAdminCalls_ReturnsPagedResult_200()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserSummaryResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserDetails_WhenUserNotFound_ReturnsNotFound_404()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStatus_WhenAdminSuspendsUser_UpdatesDatabaseAndReturns_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync();

        var targetUserId = Guid.NewGuid();
        var targetUser = new User
        {
            Id = targetUserId,
            Email = $"target_{Guid.NewGuid():N}@test.com",
            FullName = "Target Customer",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(targetUser);
        await DbContext.SaveChangesAsync();

        var suspendRequest = new UpdateUserStatusRequest
        {
            IsSuspended = true,
            IsCustomerProfile = false,
            Reason = "Vi phạm điều khoản cộng đồng"
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/users/{targetUserId}/status", suspendRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify DB
        DbContext.ChangeTracker.Clear();
        var updatedInDb = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetUserId);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.Status.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public async Task UpdateStatus_WhenUserAlreadySuspended_ReturnsConflict_409()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var targetUserId = Guid.NewGuid();
        var targetUser = new User
        {
            Id = targetUserId,
            Email = $"suspended_{Guid.NewGuid():N}@test.com",
            FullName = "Suspended Customer",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Suspended,
            CustomerStatus = CustomerStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(targetUser);
        await DbContext.SaveChangesAsync();

        var duplicateSuspendRequest = new UpdateUserStatusRequest
        {
            IsSuspended = true,
            IsCustomerProfile = false,
            Reason = "Thử suspend lại lần 2"
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/users/{targetUserId}/status", duplicateSuspendRequest);

        // Assert (Service ném Conflict 409: User account is already suspended.)
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateStatus_WhenTargetIsAdmin_ReturnsForbidden_403()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync();

        var anotherAdminId = Guid.NewGuid();
        var anotherAdmin = new User
        {
            Id = anotherAdminId,
            Email = $"other_admin_{Guid.NewGuid():N}@test.com",
            FullName = "Other Admin",
            PasswordHash = "dummyhash",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(anotherAdmin);
        await DbContext.SaveChangesAsync();

        var request = new UpdateUserStatusRequest
        {
            IsSuspended = true,
            IsCustomerProfile = false,
            Reason = "Cannot suspend admin"
        };

        // Act
        var response = await Client.PatchAsJsonAsync($"/api/users/{anotherAdminId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
