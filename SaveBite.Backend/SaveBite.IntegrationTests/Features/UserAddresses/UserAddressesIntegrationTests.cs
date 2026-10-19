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

namespace SaveBite.IntegrationTests.Features.UserAddresses;

public class UserAddressesIntegrationTests : BaseIntegrationTest
{
    public UserAddressesIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyAddresses_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Act
        var response = await Client.GetAsync("/api/user-addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAddress_WhenValidRequest_CreatesAddressAndReturns_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var request = new UserAddressRequests.CreateAddressRequest
        {
            Label = "Nhà riêng",
            AddressLine = "123 Đường Nguyễn Huệ",
            Ward = "Bến Nghé",
            District = "Quận 1",
            City = "Hồ Chí Minh",
            Latitude = 10.7769,
            Longitude = 106.7009,
            IsDefault = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/user-addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserAddressResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.AddressLine.Should().Be("123 Đường Nguyễn Huệ");
        body.Data.IsDefault.Should().BeTrue();

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var addressInDb = await DbContext.UserAddresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == body.Data.Id);
        addressInDb.Should().NotBeNull();
        addressInDb!.UserId.Should().Be(customer.Id);
        addressInDb.Label.Should().Be("Nhà riêng");
    }

    [Fact]
    public async Task GetMyAddresses_WhenCalled_ReturnsOnlyCurrentUserAddresses_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        // Thêm 1 địa chỉ của customer hiện tại
        var myAddress = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Label = "Văn phòng",
            AddressLine = "456 Lê Duẩn",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Thêm 1 user khác và 1 địa chỉ của user đó
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"other_{Guid.NewGuid():N}@test.com",
            FullName = "Other User",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(otherUser);

        var otherUserAddress = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = otherUser.Id,
            Label = "Nhà người khác",
            AddressLine = "999 Khác",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.UserAddresses.AddRange(myAddress, otherUserAddress);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/user-addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<UserAddressResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Should().ContainSingle(a => a.Id == myAddress.Id);
        body.Data.Should().NotContain(a => a.Id == otherUserAddress.Id);
    }

    [Fact]
    public async Task SetDefaultAddress_WhenAddressIsNotDefault_SetsToDefault_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var defaultAddress = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Label = "Địa chỉ 1 (Cũ)",
            AddressLine = "111 Phố Huế",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var newDefaultCandidate = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Label = "Địa chỉ 2 (Mới)",
            AddressLine = "222 Hàng Bài",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.UserAddresses.AddRange(defaultAddress, newDefaultCandidate);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PutAsync($"/api/user-addresses/{newDefaultCandidate.Id}/default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: Địa chỉ 2 thành default, Địa chỉ 1 mất default
        DbContext.ChangeTracker.Clear();
        var address2InDb = await DbContext.UserAddresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == newDefaultCandidate.Id);
        var address1InDb = await DbContext.UserAddresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == defaultAddress.Id);

        address2InDb.Should().NotBeNull();
        address2InDb!.IsDefault.Should().BeTrue();
        address1InDb!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefaultAddress_WhenAlreadyDefault_ReturnsConflict_409()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var defaultAddress = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Label = "Địa chỉ mặc định",
            AddressLine = "123 Cầu Giấy",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.UserAddresses.Add(defaultAddress);
        await DbContext.SaveChangesAsync();

        // Act: Cố tình đặt lại default cho chính địa chỉ đã là default
        var response = await Client.PutAsync($"/api/user-addresses/{defaultAddress.Id}/default", null);

        // Assert: 409 Conflict ("The default address is already the default address.")
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAddress_WhenValidRequest_UpdatesDatabaseAndReturns_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var address = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Label = "Nhà",
            AddressLine = "123 Đường Cũ",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.UserAddresses.Add(address);
        await DbContext.SaveChangesAsync();

        var updateRequest = new UserAddressRequests.UpdateAddressRequest
        {
            Label = "Văn phòng mới",
            AddressLine = "456 Đường Mới",
            IsDefault = false
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/user-addresses/{address.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var updatedInDb = await DbContext.UserAddresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == address.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.Label.Should().Be("Văn phòng mới");
        updatedInDb.AddressLine.Should().Be("456 Đường Mới");
    }

    [Fact]
    public async Task DeleteAddress_WhenAddressIsDefault_ReturnsConflict_409()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var defaultAddress = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Label = "Địa chỉ mặc định duy nhất",
            AddressLine = "789 Phố Mới",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.UserAddresses.Add(defaultAddress);
        await DbContext.SaveChangesAsync();

        // Act: Không cho phép xóa địa chỉ mặc định
        var response = await Client.DeleteAsync($"/api/user-addresses/{defaultAddress.Id}");

        // Assert: 409 Conflict ("The default address cannot be deleted.")
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteAddress_WhenAddressIsNotDefault_DeletesSuccessfully_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var defaultAddress = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Label = "Địa chỉ mặc định",
            AddressLine = "111 Đỗ Đức Dục",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var secondaryAddress = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            Label = "Địa chỉ phụ",
            AddressLine = "222 Phạm Hùng",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.UserAddresses.AddRange(defaultAddress, secondaryAddress);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/user-addresses/{secondaryAddress.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: secondaryAddress bị xóa thật khỏi DB
        DbContext.ChangeTracker.Clear();
        var exists = await DbContext.UserAddresses.AnyAsync(a => a.Id == secondaryAddress.Id);
        exists.Should().BeFalse();
    }
}
