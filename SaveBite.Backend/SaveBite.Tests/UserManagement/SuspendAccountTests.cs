using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.UserManagement;

/// <summary>
/// Unit Tests for Feature: Suspend Account
/// Service: UserService.UpdateStatusAsync
/// Function Code: UT-34
/// Sheet Name: UT_SuspendAccount
/// </summary>
public class SuspendAccountTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock = new();
    private readonly UserService _service;

    // Fixed Test Identifiers
    private readonly Guid _adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _existingUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _nonExistentUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public SuspendAccountTests(ITestOutputHelper output)
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

    private User CreateSampleUser(UserRole role = UserRole.User, UserStatus status = UserStatus.Active)
    {
        return new User
        {
            Id = _existingUserId,
            Email = "target.user@savebite.vn",
            FullName = "Target User",
            Role = role,
            Status = status,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    // =========================================================================
    // TCID01: Khóa tài khoản thành công khi user hợp lệ, không phải Admin, đang Active (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_SuspendAccount_ValidActiveUser_SuspendsSuccessfully_Normal()
    {
        // Arrange
        var user = CreateSampleUser(UserRole.User, UserStatus.Active);
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = false,
            IsSuspended = true,
            Reason = "Violation of platform policies"
        };

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _service.UpdateStatusAsync(_adminId, _existingUserId, request);

        // Assert & Log
        PrintSuccess("TCID01", "Account suspended successfully");
        user.Status.Should().Be(UserStatus.Suspended);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _userRepoMock.Verify(r => r.AddAccountStatusLog(It.Is<AccountStatusLog>(l =>
            l.UserId == _existingUserId &&
            l.AdminId == _adminId &&
            l.Action == UserStatus.Suspended &&
            l.Reason == request.Reason)), Times.Once);

        _userRepoMock.Verify(r => r.AddAuditLog(It.Is<AuditLog>(l =>
            l.ActorUserId == _adminId &&
            l.TargetId == _existingUserId &&
            l.Action == "SuspendAccount")), Times.Once);

        _refreshTokenServiceMock.Verify(r => r.RevokeAllAsync(
            _existingUserId,
            "Account suspended by Administrator.",
            It.IsAny<CancellationToken>()), Times.Once);

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Không tìm thấy người dùng trong DB -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_SuspendAccount_UserNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = false,
            IsSuspended = true,
            Reason = "Violation of platform policies"
        };

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _service.UpdateStatusAsync(_adminId, _nonExistentUserId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("User not found.");

        _refreshTokenServiceMock.Verify(r => r.RevokeAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID03: Người dùng có vai trò là Administrator -> Ném 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_SuspendAccount_TargetIsAdmin_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var adminUser = CreateSampleUser(UserRole.Admin, UserStatus.Active);
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = false,
            IsSuspended = true,
            Reason = "Attempt to suspend admin"
        };

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        // Act
        var act = () => _service.UpdateStatusAsync(_adminId, _existingUserId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ex.Which.Message.Should().Be("Cannot modify Administrator accounts.");

        _refreshTokenServiceMock.Verify(r => r.RevokeAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID04: Tài khoản người dùng đã ở trạng thái Suspended -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_SuspendAccount_AlreadySuspended_ThrowsConflict_Abnormal()
    {
        // Arrange
        var suspendedUser = CreateSampleUser(UserRole.User, UserStatus.Suspended);
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = false,
            IsSuspended = true,
            Reason = "Already suspended user"
        };

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedUser);

        // Act
        var act = () => _service.UpdateStatusAsync(_adminId, _existingUserId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("User account is already suspended.");

        _refreshTokenServiceMock.Verify(r => r.RevokeAllAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================
    // Helper Methods for Formatting Log Output
    // =========================================================
    private void PrintSuccess(string tcid, string message, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine($"MESSAGE: {message}");
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
