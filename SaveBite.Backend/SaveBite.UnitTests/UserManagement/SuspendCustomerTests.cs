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
/// Unit Tests for Feature: Suspend Customer
/// Service: UserService.UpdateStatusAsync
/// Function Code: UT-36
/// Sheet Name: UT_SuspendCustomer
/// </summary>
public class SuspendCustomerTests
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

    public SuspendCustomerTests(ITestOutputHelper output)
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

    private User CreateSampleUser(UserRole role = UserRole.User, CustomerStatus customerStatus = CustomerStatus.Active)
    {
        return new User
        {
            Id = _existingUserId,
            Email = "customer.target@savebite.vn",
            FullName = "Customer Target",
            Role = role,
            Status = UserStatus.Active,
            CustomerStatus = customerStatus,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    // =========================================================================
    // TCID01: Khóa quyền khách hàng thành công khi user hợp lệ, không phải Admin, CustomerStatus là Active (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_SuspendCustomer_ValidActiveCustomer_SuspendsSuccessfully_Normal()
    {
        // Arrange
        var user = CreateSampleUser(UserRole.User, CustomerStatus.Active);
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = true,
            IsSuspended = true,
            Reason = "Violation of order placement policy"
        };

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _service.UpdateStatusAsync(_adminId, _existingUserId, request);

        // Assert & Log
        PrintSuccess("TCID01", "Customer profile suspended successfully");
        user.CustomerStatus.Should().Be(CustomerStatus.Suspended);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _userRepoMock.Verify(r => r.AddAuditLog(It.Is<AuditLog>(l =>
            l.ActorUserId == _adminId &&
            l.TargetId == _existingUserId &&
            l.Action == "SuspendCustomer" &&
            l.Reason == request.Reason)), Times.Once);

        _refreshTokenServiceMock.Verify(r => r.RevokeAllAsync(
            _existingUserId,
            "Customer profile suspended by Administrator.",
            It.IsAny<CancellationToken>()), Times.Once);

        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Không tìm thấy người dùng trong DB -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID02_SuspendCustomer_UserNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = true,
            IsSuspended = true,
            Reason = "Violation of policy"
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
    public async Task TCID03_SuspendCustomer_TargetIsAdmin_ThrowsForbidden_Abnormal()
    {
        // Arrange
        var adminUser = CreateSampleUser(UserRole.Admin, CustomerStatus.Active);
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = true,
            IsSuspended = true,
            Reason = "Attempt to suspend admin customer profile"
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
    // TCID04: Hồ sơ khách hàng đã ở trạng thái Suspended -> Ném 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID04_SuspendCustomer_AlreadySuspended_ThrowsConflict_Abnormal()
    {
        // Arrange
        var suspendedCustomer = CreateSampleUser(UserRole.User, CustomerStatus.Suspended);
        var request = new UpdateUserStatusRequest
        {
            IsCustomerProfile = true,
            IsSuspended = true,
            Reason = "Already suspended customer"
        };

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedCustomer);

        // Act
        var act = () => _service.UpdateStatusAsync(_adminId, _existingUserId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("Customer profile is already suspended.");

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
