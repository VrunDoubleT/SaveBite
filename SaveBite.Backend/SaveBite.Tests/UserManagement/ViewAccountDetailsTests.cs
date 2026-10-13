using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.UserManagement;

/// <summary>
/// Unit Tests for Feature: View Account Details
/// Service: UserService.GetUserDetailsAsync
/// Function Code: UT-33
/// Sheet Name: UT_ViewAccountDetails
/// </summary>
public class ViewAccountDetailsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock = new();
    private readonly UserService _service;

    // Fixed Test Identifiers
    private readonly Guid _existingUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _nonExistentUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private readonly Guid _adminActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public ViewAccountDetailsTests(ITestOutputHelper output)
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

    private User CreateSampleUser()
    {
        return new User
        {
            Id = _existingUserId,
            Email = "customer.test@savebite.vn",
            Phone = "0987654321",
            FullName = "Nguyen Van Test",
            AvatarUrl = "https://res.cloudinary.com/savebite/avatar.png",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    // =========================================================================
    // TCID01: Người dùng tồn tại, có lịch sử nhật ký (user Exists, logs Has items) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetUserDetails_UserExists_HasLogs_ReturnsUserDetails_Normal()
    {
        // Arrange
        var user = CreateSampleUser();
        var auditLogs = new List<AuditLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ActorUserId = _adminActorId,
                Action = "ReactivateAccount",
                Reason = "Periodic review completed",
                NewValuesJson = "{\"Status\":\"Active\"}",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };
        var roleLogs = new List<UserRoleChangeLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _existingUserId,
                PreviousRole = UserRole.User,
                NewRole = UserRole.StoreOwner,
                Reason = "Approved store owner application",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepoMock
            .Setup(r => r.GetUserAuditLogsAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditLogs);

        _userRepoMock
            .Setup(r => r.GetUserRoleLogsAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleLogs);

        // Act
        var result = await _service.GetUserDetailsAsync(_existingUserId);

        // Assert & Log
        PrintSuccess("TCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingUserId);
        result.Email.Should().Be("customer.test@savebite.vn");
        result.FullName.Should().Be("Nguyen Van Test");
        result.Role.Should().Be(UserRole.User.ToString());
        result.Status.Should().Be(UserStatus.Active.ToString());
        result.Logs.Should().HaveCount(1);
        result.Logs.First().Action.Should().Be("ReactivateAccount");
        result.RoleLogs.Should().HaveCount(1);
        result.RoleLogs.First().NewRole.Should().Be(UserRole.StoreOwner.ToString());

        _userRepoMock.Verify(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.GetUserAuditLogsAsync(_existingUserId, It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.GetUserRoleLogsAsync(_existingUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Người dùng tồn tại, chưa có nhật ký nào (user Exists, logs Empty) (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetUserDetails_UserExists_EmptyLogs_ReturnsUserDetails_Normal()
    {
        // Arrange
        var user = CreateSampleUser();

        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepoMock
            .Setup(r => r.GetUserAuditLogsAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditLog>());

        _userRepoMock
            .Setup(r => r.GetUserRoleLogsAsync(_existingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRoleChangeLog>());

        // Act
        var result = await _service.GetUserDetailsAsync(_existingUserId);

        // Assert & Log
        PrintSuccess("TCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.Id.Should().Be(_existingUserId);
        result.Logs.Should().BeEmpty();
        result.RoleLogs.Should().BeEmpty();

        _userRepoMock.Verify(r => r.GetUserByIdAsync(_existingUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID03: Người dùng không tồn tại trong hệ thống (user Not Found) -> Ném 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task TCID03_GetUserDetails_UserNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetUserByIdAsync(_nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _service.GetUserDetailsAsync(_nonExistentUserId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("TCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("User not found.");

        _userRepoMock.Verify(r => r.GetUserAuditLogsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepoMock.Verify(r => r.GetUserRoleLogsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
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
