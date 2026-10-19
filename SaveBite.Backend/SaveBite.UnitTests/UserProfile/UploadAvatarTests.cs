using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
/// Unit Tests for Feature: Upload Avatar
/// Service: UserService.UploadAvatarAsync
/// Function Code: UT-16
/// Sheet Name: UT_UploadAvatar
/// </summary>
public class UploadAvatarTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock = new();
    private readonly UserService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public UploadAvatarTests(ITestOutputHelper output)
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

    private User CreateValidUser(string? avatarUrl = "https://res.cloudinary.com/savebite/image/upload/v12345/avatars/old_avatar.png")
    {
        return new User
        {
            Id = _userId,
            Email = "customer@savebite.vn",
            Phone = "0987654321",
            FullName = "Nguyen Van Customer",
            AvatarUrl = avatarUrl,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active
        };
    }

    private static IFormFile CreateMockFormFile(string fileName = "avatar.png")
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(1024);
        return fileMock.Object;
    }

    // =========================================================================
    // UTCID01: Upload avatar thành công khi đã có avatar Cloudinary cũ -> Cập nhật URL mới và xóa avatar cũ (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_UploadAvatar_HasExistingCloudinaryAvatar_ShouldUpdateAndRemoveOld_Normal()
    {
        // Arrange
        var oldUrl = "https://res.cloudinary.com/savebite/image/upload/v12345/avatars/old_avatar.png";
        var user = CreateValidUser(avatarUrl: oldUrl);
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var file = CreateMockFormFile();
        var newUploadResult = new CloudinaryUploadResult("https://res.cloudinary.com/savebite/image/upload/v67890/avatars/new_avatar.png", "avatars/new_avatar");

        _cloudinaryServiceMock
            .Setup(c => c.UploadImageAsync(file, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUploadResult);

        _cloudinaryServiceMock
            .Setup(c => c.DeleteImageAsync("avatars/old_avatar"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UploadAvatarAsync(_userId, file);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.AvatarUrl.Should().Be(newUploadResult.Url);

        user.AvatarUrl.Should().Be(newUploadResult.Url);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryServiceMock.Verify(c => c.DeleteImageAsync("avatars/old_avatar"), Times.Once);
    }

    // =========================================================================
    // UTCID02: Upload avatar thành công khi chưa có avatar trước đó (null) -> Cập nhật mới và không gọi xóa (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_UploadAvatar_NoExistingAvatar_ShouldUpdateWithoutDeleting_Normal()
    {
        // Arrange
        var user = CreateValidUser(avatarUrl: null);
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var file = CreateMockFormFile();
        var newUploadResult = new CloudinaryUploadResult("https://res.cloudinary.com/savebite/image/upload/v67890/avatars/first_avatar.png", "avatars/first_avatar");

        _cloudinaryServiceMock
            .Setup(c => c.UploadImageAsync(file, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUploadResult);

        // Act
        var result = await _service.UploadAvatarAsync(_userId, file);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.AvatarUrl.Should().Be(newUploadResult.Url);

        user.AvatarUrl.Should().Be(newUploadResult.Url);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryServiceMock.Verify(c => c.DeleteImageAsync(It.IsAny<string>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: Upload avatar thành công khi avatar cũ không phải định dạng Cloudinary -> Không gọi xóa (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID03_UploadAvatar_OldAvatarNotCloudinary_ShouldNotDeleteOldImage_Normal()
    {
        // Arrange
        var nonCloudinaryUrl = "https://example.com/external-avatar.jpg";
        var user = CreateValidUser(avatarUrl: nonCloudinaryUrl);
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var file = CreateMockFormFile();
        var newUploadResult = new CloudinaryUploadResult("https://res.cloudinary.com/savebite/image/upload/v67890/avatars/new_avatar.png", "avatars/new_avatar");

        _cloudinaryServiceMock
            .Setup(c => c.UploadImageAsync(file, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUploadResult);

        // Act
        var result = await _service.UploadAvatarAsync(_userId, file);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.AvatarUrl.Should().Be(newUploadResult.Url);

        user.AvatarUrl.Should().Be(newUploadResult.Url);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cloudinaryServiceMock.Verify(c => c.DeleteImageAsync(It.IsAny<string>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: File upload không hợp lệ (Cloudinary ném ArgumentException) -> Bắt và ném 400 BadRequest (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID04_UploadAvatar_InvalidFile_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var user = CreateValidUser();
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var file = CreateMockFormFile("malicious.exe");
        _cloudinaryServiceMock
            .Setup(c => c.UploadImageAsync(file, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("File format not supported."));

        // Act
        var act = () => _service.UploadAvatarAsync(_userId, file);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Which.Message.Should().Be("File format not supported.");

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: Người dùng không tồn tại trong DB -> Ném 401 Unauthorized (Abnormal)
    // =========================================================================
    [Fact]
    public async Task UTCID05_UploadAvatar_UserNotFound_ThrowsUnauthorized_Abnormal()
    {
        // Arrange
        var nonExistentUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var file = CreateMockFormFile();

        // Act
        var act = () => _service.UploadAvatarAsync(nonExistentUserId, file);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID05", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ex.Which.ErrorCode.Should().Be(ErrorCodes.Unauthorized);

        _cloudinaryServiceMock.Verify(c => c.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()), Times.Never);
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
