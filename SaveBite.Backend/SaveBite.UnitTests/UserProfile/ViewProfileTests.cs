using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.UserProfile;

/// <summary>
/// Unit Tests for Feature: View Profile
/// Service: UserService.GetCurrentUserAsync
/// Function Code: UT-14
/// Sheet Name: UT_GetCurrentUser
/// </summary>
public class ViewProfileTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock = new();
    private readonly UserService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ViewProfileTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new UserService(
            _userRepoMock.Object,
            _refreshTokenServiceMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            _cloudinaryServiceMock.Object
        );
    }

    private User CreateValidUser(string? phone = "0987654321", string? avatarUrl = "https://res.cloudinary.com/savebite/image/upload/v1/avatars/user1.png")
    {
        return new User
        {
            Id = _userId,
            Email = "customer@savebite.vn",
            Phone = phone,
            FullName = "Nguyen Van Customer",
            AvatarUrl = avatarUrl,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Addresses = new List<UserAddress>()
        };
    }

    // =========================================================================
    // UTCID01: Xem thông tin hồ sơ thành công khi người dùng có địa chỉ mặc định (IsDefault == true) -> Trả về thông tin kèm địa chỉ mặc định chính xác (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetCurrentUser_WithDefaultAddress_ShouldReturnProfileWithDefaultAddress_Normal()
    {
        // Arrange
        var user = CreateValidUser();
        var defaultAddress = new UserAddress
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            UserId = _userId,
            Label = "Home",
            AddressLine = "123 Le Loi Street",
            Ward = "Ben Nghe Ward",
            District = "District 1",
            City = "Ho Chi Minh City",
            Latitude = 10.7769,
            Longitude = 106.7009,
            IsDefault = true
        };
        var secondaryAddress = new UserAddress
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserId = _userId,
            Label = "Office",
            AddressLine = "456 Nguyen Hue Boulevard",
            Ward = "Ben Nghe Ward",
            District = "District 1",
            City = "Ho Chi Minh City",
            Latitude = 10.7745,
            Longitude = 106.7025,
            IsDefault = false
        };

        // Thứ tự trong danh sách cố tình để secondaryAddress trước để kiểm tra filter đúng IsDefault
        user.Addresses.Add(secondaryAddress);
        user.Addresses.Add(defaultAddress);

        _userRepoMock
            .Setup(r => r.GetUserWithAddressesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetCurrentUserAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_userId);
        result.Email.Should().Be("customer@savebite.vn");
        result.FullName.Should().Be("Nguyen Van Customer");
        result.Phone.Should().Be("0987654321");
        result.AvatarUrl.Should().Be("https://res.cloudinary.com/savebite/image/upload/v1/avatars/user1.png");
        result.Role.Should().Be(UserRole.User.ToString());
        result.CustomerStatus.Should().Be(CustomerStatus.Active.ToString());

        result.DefaultAddress.Should().NotBeNull();
        result.DefaultAddress!.Id.Should().Be(defaultAddress.Id);
        result.DefaultAddress.Label.Should().Be("Home");
        result.DefaultAddress.AddressLine.Should().Be("123 Le Loi Street");
        result.DefaultAddress.Ward.Should().Be("Ben Nghe Ward");
        result.DefaultAddress.District.Should().Be("District 1");
        result.DefaultAddress.City.Should().Be("Ho Chi Minh City");
        result.DefaultAddress.Latitude.Should().Be(10.7769);
        result.DefaultAddress.Longitude.Should().Be(106.7009);
        result.DefaultAddress.IsDefault.Should().BeTrue();

        _userRepoMock.Verify(r => r.GetUserWithAddressesAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Xem thông tin hồ sơ khi người dùng có địa chỉ nhưng không có địa chỉ nào đánh dấu IsDefault == true -> Lấy địa chỉ đầu tiên làm fallback DefaultAddress (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetCurrentUser_WithoutExplicitDefaultAddress_ShouldFallbackToFirstAddress_Normal()
    {
        // Arrange
        var user = CreateValidUser();
        var firstAddress = new UserAddress
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            UserId = _userId,
            Label = "Company HQ",
            AddressLine = "789 Vo Van Tan",
            Ward = "Ward 6",
            District = "District 3",
            City = "Ho Chi Minh City",
            Latitude = 10.7780,
            Longitude = 106.6900,
            IsDefault = false
        };
        var secondAddress = new UserAddress
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            UserId = _userId,
            Label = "Warehouse",
            AddressLine = "101 Pham Van Dong",
            Ward = "Hiep Binh Chanh",
            District = "Thu Duc",
            City = "Ho Chi Minh City",
            Latitude = 10.8250,
            Longitude = 106.7200,
            IsDefault = false
        };

        user.Addresses.Add(firstAddress);
        user.Addresses.Add(secondAddress);

        _userRepoMock
            .Setup(r => r.GetUserWithAddressesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetCurrentUserAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.DefaultAddress.Should().NotBeNull();
        result.DefaultAddress!.Id.Should().Be(firstAddress.Id);
        result.DefaultAddress.Label.Should().Be("Company HQ");
        result.DefaultAddress.AddressLine.Should().Be("789 Vo Van Tan");
        result.DefaultAddress.IsDefault.Should().BeFalse();

        _userRepoMock.Verify(r => r.GetUserWithAddressesAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Xem thông tin hồ sơ khi người dùng chưa có bất kỳ địa chỉ nào -> Trả về hồ sơ thành công với DefaultAddress là null (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_GetCurrentUser_NoAddresses_ShouldReturnProfileWithNullDefaultAddress_Normal()
    {
        // Arrange
        var user = CreateValidUser();
        user.Addresses.Clear();

        _userRepoMock
            .Setup(r => r.GetUserWithAddressesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetCurrentUserAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_userId);
        result.DefaultAddress.Should().BeNull();

        _userRepoMock.Verify(r => r.GetUserWithAddressesAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID04: Xem thông tin hồ sơ của người dùng không có số điện thoại và avatar (Phone == null, AvatarUrl == null) -> Trả về thông tin đúng (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID04_GetCurrentUser_OptionalFieldsNull_ShouldReturnProfileSuccessfully_Normal()
    {
        // Arrange
        var user = CreateValidUser(phone: null, avatarUrl: null);

        _userRepoMock
            .Setup(r => r.GetUserWithAddressesAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetCurrentUserAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID04", result, "200 OK");
        result.Should().NotBeNull();
        result.Phone.Should().BeNull();
        result.AvatarUrl.Should().BeNull();
        result.DefaultAddress.Should().BeNull();

        _userRepoMock.Verify(r => r.GetUserWithAddressesAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID05: Người dùng không tồn tại trong hệ thống (Repository trả về null) -> Ném AppException 401 Unauthorized (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID05_GetCurrentUser_UserNotFound_ThrowsUnauthorized_Abnormal()
    {
        // Arrange
        var nonExistentUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        _userRepoMock
            .Setup(r => r.GetUserWithAddressesAsync(nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _service.GetCurrentUserAsync(nonExistentUserId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.Unauthorized);

        _userRepoMock.Verify(r => r.GetUserWithAddressesAsync(nonExistentUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================
    // Helper Methods for Formatting Log Output
    // =========================================================
    private void PrintSuccess<T>(string tcid, T result, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }

    private void PrintException(string tcid, AppException ex)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"ERROR CODE: {ex.ErrorCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }
}
