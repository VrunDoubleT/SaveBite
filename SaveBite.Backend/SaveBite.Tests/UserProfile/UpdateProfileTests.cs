using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.UserProfile;

/// <summary>
/// Unit Tests for Feature: Update Profile
/// Service: UserService.UpdateProfileAsync
/// Function Code: UT-15
/// Sheet Name: UT_UpdateProfile
/// </summary>
public class UpdateProfileTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock = new();
    private readonly UserService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public UpdateProfileTests(ITestOutputHelper output)
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

    private User CreateValidUser(string fullName = "Old Name", string? phone = "0987654321")
    {
        return new User
        {
            Id = _userId,
            Email = "customer@savebite.vn",
            Phone = phone,
            FullName = fullName,
            AvatarUrl = "https://res.cloudinary.com/savebite/image/upload/v1/avatars/user1.png",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    // =========================================================================
    // UTCID01: Cập nhật thông tin hợp lệ đầy đủ FullName và Phone -> Thành công (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_UpdateProfile_ValidFullNameAndPhone_ShouldUpdateSuccessfully_Normal()
    {
        // Arrange
        var user = CreateValidUser();
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new UpdateUserProfileRequest
        {
            FullName = "Tran Van B",
            Phone = "0912345678"
        };

        // Act
        var result = await _service.UpdateProfileAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.FullName.Should().Be("Tran Van B");
        result.Phone.Should().Be("0912345678");

        user.FullName.Should().Be("Tran Van B");
        user.Phone.Should().Be("0912345678");

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Cập nhật với Phone là null -> Thành công, user.Phone được lưu là null (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_UpdateProfile_PhoneNull_ShouldSetPhoneToNull_Normal()
    {
        // Arrange
        var user = CreateValidUser(phone: "0987654321");
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new UpdateUserProfileRequest
        {
            FullName = "Tran Van B",
            Phone = null
        };

        // Act
        var result = await _service.UpdateProfileAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.FullName.Should().Be("Tran Van B");
        result.Phone.Should().BeNull();

        user.FullName.Should().Be("Tran Van B");
        user.Phone.Should().BeNull();

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Cập nhật với Phone là chuỗi khoảng trắng -> Thành công, chuyển thành null (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_UpdateProfile_PhoneWhitespace_ShouldSetPhoneToNull_Normal()
    {
        // Arrange
        var user = CreateValidUser(phone: "0987654321");
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new UpdateUserProfileRequest
        {
            FullName = "Tran Van B",
            Phone = "   "
        };

        // Act
        var result = await _service.UpdateProfileAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.FullName.Should().Be("Tran Van B");
        result.Phone.Should().BeNull();

        user.FullName.Should().Be("Tran Van B");
        user.Phone.Should().BeNull();

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID04: Cập nhật FullName và Phone có khoảng trắng ở đầu/cuối -> Tự động Trim() (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID04_UpdateProfile_FieldsWithLeadingTrailingSpaces_ShouldTrimValues_Normal()
    {
        // Arrange
        var user = CreateValidUser();
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new UpdateUserProfileRequest
        {
            FullName = "  Tran Van B  ",
            Phone = " 0912345678 "
        };

        // Act
        var result = await _service.UpdateProfileAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID04", result, "200 OK");
        result.Should().NotBeNull();
        result.FullName.Should().Be("Tran Van B");
        result.Phone.Should().Be("0912345678");

        user.FullName.Should().Be("Tran Van B");
        user.Phone.Should().Be("0912345678");

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID05: Người dùng không tồn tại trong DB -> Ném 401 Unauthorized (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID05_UpdateProfile_UserNotFound_ThrowsUnauthorized_Abnormal()
    {
        // Arrange
        var nonExistentUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = new UpdateUserProfileRequest
        {
            FullName = "Tran Van B",
            Phone = "0912345678"
        };

        // Act
        var act = () => _service.UpdateProfileAsync(nonExistentUserId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.Unauthorized);

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
