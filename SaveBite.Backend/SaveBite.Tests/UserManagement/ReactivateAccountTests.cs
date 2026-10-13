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
/// Unit Tests for Feature: Reactivate Account
/// Service: UserService.UpdateStatusAsync
/// Function Code: UT-35
/// Sheet Name: UT_ReactivateAccount
/// </summary>
public class ReactivateAccountTests
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

    public ReactivateAccountTests(ITestOutputHelper output)
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

    private User CreateSampleUser(UserRole role = UserRole.User, UserStatus status = UserStatus.Suspended)
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
    // TCID01: Mở khóa tài khoản thành công khi user hợp lệ, không phải Admin, đang Suspended (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_ReactivateAccount_ValidSuspendedUser_ReactivatesSuccessfully_Normal()
    {
        // Arrange
        var user = CreateSampleUser(UserRole.User, UserStatus.Suspended);
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = false,
            IsSuspended = false,
            Reason = "Account verified and reactivated"
        };

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _service.UpdateStatusAsync(_adminId, _existingUserId, request);

        // Assert & Log
        PrintSuccess("TCID01", "Account reactivated successfully");
        user.Status.Should().Be(UserStatus.Active);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _userRepoMock.Verify(r => r.AddAccountStatusLog(It.Is<AccountStatusLog>(l =>
            l.UserId == _existingUserId &&
            l.AdminId == _adminId &&
            l.Action == UserStatus.Active &&
            l.Reason == request.Reason)), Times.Once);

        _userRepoMock.Verify(r => r.AddAuditLog(It.Is<AuditLog>(l =>
            l.ActorUserId == _adminId &&
            l.TargetId == _existingUserId &&
            l.Action == "ReactivateAccount")), Times.Once);

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Không tìm thấy người dùng trong DB -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_ReactivateAccount_UserNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = false,
            IsSuspended = false,
            Reason = "Reactivate non-existent user"
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

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID03: Người dùng có vai trò là Administrator -> Ném 403 Forbidden (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_ReactivateAccount_TargetIsAdmin_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var adminUser = CreateSampleUser(UserRole.Admin, UserStatus.Suspended);
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = false,
            IsSuspended = false,
            Reason = "Attempt to reactivate admin"
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

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // TCID04: Tài khoản người dùng đã ở trạng thái Active -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_ReactivateAccount_AlreadyActive_ThrowsConflict_Abnormal()
    {
        // Arrange
        var activeUser = CreateSampleUser(UserRole.User, UserStatus.Active);
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = false,
            IsSuspended = false,
            Reason = "Already active user"
        };

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeUser);

        // Act
        var act = () => _service.UpdateStatusAsync(_adminId, _existingUserId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("User account is already active.");

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
